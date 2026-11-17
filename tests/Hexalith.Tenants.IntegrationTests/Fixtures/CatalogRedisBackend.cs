using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

using StackExchange.Redis;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Uses one validated Redis database for state, pub/sub, authority and direct product assertions.</summary>
internal static class CatalogRedisBackend
{
    /// <summary>Connects only to the backend selected by the current profile's private receipt.</summary>
    internal static Task<ConnectionMultiplexer> ConnectAsync(CancellationToken cancellationToken)
    {
        _ = DatabaseNumber();
        string? endpoint = Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_REDIS_ENDPOINT");
        ConfigurationOptions options = RedisOptions(endpoint);
        if (!AspireTopologyFixture.IsSourceCatalogProfile && endpoint is null)
        {
            options.EndPoints.Clear();
            options.EndPoints.Add("localhost", DaprDiagnostics.ResolveRedisPort());
        }
        return ConnectionMultiplexer.ConnectAsync(options).WaitAsync(cancellationToken);
    }

    /// <summary>Reads the exact logical database without falling back to DB0 for a cold profile.</summary>
    internal static IDatabase Database(IConnectionMultiplexer connection)
    {
        int database = DatabaseNumber();
        if (AspireTopologyFixture.IsSourceCatalogProfile)
        {
            try
            {
                using JsonDocument receipt = JsonDocument.Parse(ReadPrivate(Required("TENANTS_SOURCE_CATALOG_PROVISIONING_RECEIPT")));
                string? expected = receipt.RootElement.GetProperty("backend").GetProperty("redisRunId").GetString();
                string? actual = connection.GetServer(connection.GetEndPoints().Single()).Info("server")
                    .SelectMany(group => group).Single(item => item.Key == "run_id").Value;
                if (expected != actual || expected is null || !Regex.IsMatch(expected, "\\A[0-9a-f]{40}\\z")) throw Invalid();
            }
            catch (Exception exception) when (exception is not OperationCanceledException) { throw Invalid(); }
        }
        return connection.GetDatabase(database);
    }

    /// <summary>Accepts the legacy local endpoint and explicit numeric loopback overrides only.</summary>
    internal static ConfigurationOptions RedisOptions(string? endpoint)
    {
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            // The client classifies read-only INFO as administrative. Only the receipt-bound
            // cold verifier needs that category to compare the actual server run identity.
            AllowAdmin = AspireTopologyFixture.IsSourceCatalogProfile,
            ConnectTimeout = 5000,
            SyncTimeout = 5000,
            AsyncTimeout = 5000,
        };
        if (endpoint is null) options.EndPoints.Add("localhost", 6379);
        else if (IPEndPoint.TryParse(endpoint, out IPEndPoint? selected)
            && IPAddress.IsLoopback(selected.Address) && selected.Port > 0) options.EndPoints.Add(selected);
        else throw new InvalidOperationException("source-catalog-backend-endpoint-invalid");
        return options;
    }

    /// <summary>Validates receipt, producer, job and component identities before every direct read.</summary>
    internal static int DatabaseNumber()
    {
        if (!AspireTopologyFixture.IsSourceCatalogProfile) return 0;
        try
        {
            string database = Required("TENANTS_SOURCE_CATALOG_REDIS_DATABASE");
            if (!Regex.IsMatch(database, "\\A(?:[1-9]|1[0-5])\\z", RegexOptions.CultureInvariant)) throw Invalid();
            int selected = int.Parse(database, CultureInfo.InvariantCulture);
            string receiptPath = Required("TENANTS_SOURCE_CATALOG_PROVISIONING_RECEIPT");
            byte[] bytes = ReadPrivate(receiptPath);
            if (Hash(bytes) != Required("TENANTS_SOURCE_CATALOG_PROVISIONING_SHA256")) throw Invalid();
            using JsonDocument document = JsonDocument.Parse(bytes);
            JsonElement receipt = document.RootElement;
            RequireUnique(receipt);
            if (receipt.GetProperty("schema").GetString() != "tenants.catalog-profile.v1"
                || receipt.GetProperty("database").GetInt32() != selected
                || receipt.GetProperty("endpoint").GetString() != "127.0.0.1:6379"
                || Required("TENANTS_SOURCE_CATALOG_REDIS_ENDPOINT") != "127.0.0.1:6379"
                || receipt.GetProperty("sdkSource").GetString() != Required("TENANTS_SOURCE_SDK_SHA")
                || receipt.GetProperty("sdkProducerVersion").GetString() != Required("TENANTS_SOURCE_SDK_PRODUCER_VERSION")
                || receipt.GetProperty("sdkPackageProducer").GetProperty("revision").GetString() != Required("TENANTS_SOURCE_SDK_PRODUCER_REVISION")
                || receipt.GetProperty("sdkPackageProducer").GetProperty("version").GetString() != Required("TENANTS_SOURCE_SDK_PRODUCER_VERSION")
                || !receipt.GetProperty("materialProvisionedWithFreshBackend").GetBoolean()
                || receipt.GetProperty("backend").GetProperty("initialDatabaseSize").GetInt32() != 0) throw Invalid();
            JsonElement identity = receipt.GetProperty("identity");
            foreach ((string key, string variable) in new[]
            {
                ("repository", "GITHUB_REPOSITORY"), ("runId", "GITHUB_RUN_ID"),
                ("runAttempt", "GITHUB_RUN_ATTEMPT"), ("job", "GITHUB_JOB"),
            })
                if (identity.GetProperty(key).GetString() != Required(variable)) throw Invalid();
            string initializationPath = Required("HEXALITH_DAPR_INITIALIZATION_RECEIPT");
            byte[] initializationBytes = ReadPrivate(initializationPath);
            if (receipt.GetProperty("initializationReceipt").GetString() != initializationPath
                || receipt.GetProperty("initializationSha256").GetString() != Hash(initializationBytes)) throw Invalid();
            using JsonDocument initializationDocument = JsonDocument.Parse(initializationBytes);
            JsonElement initialization = initializationDocument.RootElement;
            RequireUnique(initialization);
            if (initialization.GetProperty("schema").GetString() != "hexalith.dapr-initialization.v1"
                || initialization.GetProperty("status").GetString() != "complete"
                || identity.EnumerateObject().Any(item => initialization.GetProperty("identity")
                    .GetProperty(item.Name).GetString() != item.Value.GetString())) throw Invalid();
            string containerId = receipt.GetProperty("backend").GetProperty("containerId").GetString()!;
            JsonElement container = initialization.GetProperty("createdContainers").EnumerateArray()
                .Single(item => item.GetProperty("Name").GetString() == "/dapr_redis");
            if (container.GetProperty("Id").GetString() != containerId
                || container.GetProperty("Created").GetString() != receipt.GetProperty("backend").GetProperty("created").GetString()
                || !container.GetProperty("Running").GetBoolean()) throw Invalid();
            string directory = receipt.GetProperty("privateDirectory").GetString()!;
            foreach (JsonProperty component in receipt.GetProperty("components").EnumerateObject())
            {
                if (Path.GetFileName(component.Name) != component.Name || !component.Name.EndsWith(".yaml", StringComparison.Ordinal) && component.Name != "secrets.json") throw Invalid();
                string path = Path.Combine(directory, "components", component.Name);
                byte[] componentBytes = ReadPrivate(path);
                if (Hash(componentBytes) != component.Value.GetString()) throw Invalid();
            }
            foreach ((string name, string variable) in new[]
            {
                ("statestore.yaml", "Dapr__StateStoreComponentPath"), ("pubsub.yaml", "Dapr__PubSubComponentPath"),
            })
            {
                string path = Path.Combine(directory, "components", name);
                if (Required(variable) != path || !receipt.GetProperty("components").TryGetProperty(name, out _)) throw Invalid();
                ValidateComponent(File.ReadAllText(path), database);
            }
            string custody = Path.Combine(directory, "custody");
            if (Required("Tenants__DevelopmentRouteCatalog__CustodyDirectory") != custody) throw Invalid();
            string authorityPath = Path.Combine(custody, "authority.backend.json");
            if (File.Exists(authorityPath))
            {
                using JsonDocument authorityDocument = JsonDocument.Parse(ReadPrivate(authorityPath));
                JsonElement authority = authorityDocument.RootElement;
                RequireUnique(authority);
                JsonElement[] metadata = authority.GetProperty("spec").GetProperty("metadata").EnumerateArray().ToArray();
                foreach ((string name, string value) in new[] { ("redisDB", database), ("redisHost", "localhost:6379") })
                {
                    JsonElement[] entries = metadata.Where(item => item.GetProperty("name").GetString() == name).ToArray();
                    if (entries.Length != 1 || entries[0].GetProperty("value").GetString() != value) throw Invalid();
                }
            }
            return selected;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw Invalid();
        }
    }

    /// <summary>Marks only a completed, awaited topology disposal for the next profile's admission.</summary>
    internal static void RecordShutdown()
    {
        if (!AspireTopologyFixture.IsSourceCatalogProfile) return;
        _ = DatabaseNumber();
        string receipt = Required("TENANTS_SOURCE_CATALOG_PROVISIONING_RECEIPT");
        string path = Required("TENANTS_SOURCE_CATALOG_SHUTDOWN_PATH");
        using JsonDocument document = JsonDocument.Parse(ReadPrivate(receipt));
        if (path != Path.Combine(document.RootElement.GetProperty("privateDirectory").GetString()!, "shutdown.json")) throw Invalid();
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using var stream = new FileStream(path, options);
        JsonSerializer.Serialize(stream, new { provisioningSha256 = Hash(ReadPrivate(receipt)), disposed = true });
    }

    private static void ValidateComponent(string text, string database)
    {
        foreach ((string name, string value) in new[] { ("redisDB", database), ("redisHost", "localhost:6379") })
        {
            MatchCollection entries = Regex.Matches(text, "(?m)^    - name: " + name + "\\s*\\n      value: \"([^\"]*)\"\\r?$");
            if (entries.Count != 1 || entries[0].Groups[1].Value != value
                || Regex.Matches(text, "(?m)^\\s*-\\s*name:\\s*[\"']?" + name + "[\"']?\\s*$").Count != 1) throw Invalid();
        }
    }

    private static byte[] ReadPrivate(string path)
    {
        string temporary = Path.GetFullPath(Required("RUNNER_TEMP")) + Path.DirectorySeparatorChar;
        string full = Path.GetFullPath(path);
        if (!Path.IsPathFullyQualified(path) || full != path || !full.StartsWith(temporary, StringComparison.Ordinal)) throw Invalid();
        for (FileSystemInfo? entry = new FileInfo(full); entry is not null && entry.FullName != temporary.TrimEnd(Path.DirectorySeparatorChar);
            entry = entry is FileInfo file ? file.Directory : ((DirectoryInfo)entry).Parent)
            if (entry.LinkTarget is not null || (entry.Attributes & FileAttributes.ReparsePoint) != 0) throw Invalid();
        var metadata = new FileInfo(full);
        if (!metadata.Exists || metadata.Length > 1024 * 1024
            || !OperatingSystem.IsWindows() && (File.GetUnixFileMode(full) & (UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute)) != 0) throw Invalid();
        return File.ReadAllBytes(full);
    }

    private static void RequireUnique(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            JsonProperty[] properties = element.EnumerateObject().ToArray();
            if (properties.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length) throw Invalid();
            foreach (JsonProperty property in properties) RequireUnique(property.Value);
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement item in element.EnumerateArray()) RequireUnique(item);
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw Invalid();
    private static InvalidOperationException Invalid() => new("source-catalog-backend-database-invalid");
}

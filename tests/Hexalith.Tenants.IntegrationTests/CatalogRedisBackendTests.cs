using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Tenants.IntegrationTests.Fixtures;

using NSubstitute;
using Shouldly;
using StackExchange.Redis;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Refuses disagreement between private receipts, actual readers, components and the cloned authority.</summary>
public sealed class CatalogRedisBackendTests
{
    /// <summary>The real client's INFO guard permits the private cold verifier before it tries a server transport.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ServerIdentityReadPassesTheRealClientGuardOnlyForTheColdProfile(bool cold)
    {
        const string profile = "TENANTS_SOURCE_CATALOG_PROFILE";
        string? previous = Environment.GetEnvironmentVariable(profile);
        // This owned listener deliberately implements no Redis protocol. It keeps the selected
        // endpoint away from another session while distinguishing client refusal from transport.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            Environment.SetEnvironmentVariable(profile, cold ? "true" : "false");
            string endpoint = listener.LocalEndpoint.ToString()!;
            ConfigurationOptions options = CatalogRedisBackend.RedisOptions(endpoint);
            options.ConnectTimeout = 100;
            options.SyncTimeout = 100;
            options.AsyncTimeout = 100;
            options.ConnectRetry = 0;
            using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(options).WaitAsync(TimeSpan.FromSeconds(5));
            try
            {
                IServer server = connection.GetServer(connection.GetEndPoints().Single());
                if (cold)
                {
                    Exception failure = Should.Throw<Exception>(() => server.Info("server"));
                    (failure is RedisConnectionException or RedisTimeoutException).ShouldBeTrue(
                        "the real INFO command must pass client authorization and reach the unavailable test transport");
                }
                else
                {
                    Should.Throw<RedisCommandException>(() => server.Info("server")).Message.ShouldContain("admin mode");
                }
            }
            finally { await connection.CloseAsync(allowCommandsToComplete: false).WaitAsync(TimeSpan.FromSeconds(5)); }
        }
        finally { Environment.SetEnvironmentVariable(profile, previous); }
    }

    /// <summary>Canonical private state, pub/sub and reader selections use the same nonzero database.</summary>
    [Fact]
    public void CompletePrivateProfileSelectsItsExactDatabase()
        => WithProfile("none", () => CatalogRedisBackend.DatabaseNumber().ShouldBe(5));

    /// <summary>Reserved, duplicate, noncanonical and substituted backend identities cannot fall back to DB0.</summary>
    [Theory]
    [InlineData("reserved-db")]
    [InlineData("noncanonical-db")]
    [InlineData("substituted-db")]
    [InlineData("pubsub-db")]
    [InlineData("duplicate-db")]
    [InlineData("authority-db")]
    [InlineData("changed-component")]
    [InlineData("changed-initialization")]
    [InlineData("producer-drift")]
    [InlineData("receipt-link")]
    public void ConflictingProfileRefusesWithoutEchoingPrivateInput(string mutation)
        => WithProfile(mutation, () => Should.Throw<InvalidOperationException>(() => CatalogRedisBackend.DatabaseNumber())
            .Message.ShouldBe("source-catalog-backend-database-invalid"));

    /// <summary>The direct reader binds the live Redis server run identity as well as the logical database.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DirectReaderRefusesAReplacementRedisAtTheSamePort(bool matching)
    {
        WithProfile("none", () =>
        {
            IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
            IServer server = Substitute.For<IServer>();
            EndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 6379);
            connection.GetEndPoints().Returns([endpoint]);
            connection.GetServer(endpoint).Returns(server);
            server.Info("server").Returns(new[] { KeyValuePair.Create("run_id", new string(matching ? 'e' : 'd', 40)) }
                .GroupBy(_ => "Server").ToArray());
            IDatabase database = Substitute.For<IDatabase>();
            connection.GetDatabase(5).Returns(database);
            if (matching) CatalogRedisBackend.Database(connection).ShouldBeSameAs(database);
            else Should.Throw<InvalidOperationException>(() => CatalogRedisBackend.Database(connection))
                .Message.ShouldBe("source-catalog-backend-database-invalid");
        });
    }

    private static void WithProfile(string mutation, Action assertion)
    {
        string root = Directory.CreateTempSubdirectory("tenants-reader-profile-").FullName;
        string privateDirectory = Path.Combine(root, "private");
        string components = Path.Combine(privateDirectory, "components");
        string custody = Path.Combine(privateDirectory, "custody");
        Directory.CreateDirectory(components);
        Directory.CreateDirectory(custody);
        var identity = new Dictionary<string, string>
        {
            ["repository"] = "Hexalith/Hexalith.Tenants", ["runId"] = "42", ["runAttempt"] = "2", ["job"] = "ci",
        };
        string initializationPath = Path.Combine(root, "hexalith-dapr-initialization.json");
        WritePrivate(initializationPath, JsonSerializer.SerializeToUtf8Bytes(new
        {
            schema = "hexalith.dapr-initialization.v1", status = "complete", identity,
            createdContainers = new[] { new { Name = "/dapr_redis", Id = new string('a', 64), Created = "2026-11-16T01:00:00Z", Running = true } },
        }));
        string component = "spec:\n  metadata:\n    - name: redisDB\n      value: \"5\"\n    - name: redisHost\n      value: \"localhost:6379\"\n";
        WritePrivate(Path.Combine(components, "statestore.yaml"), System.Text.Encoding.UTF8.GetBytes(component));
        WritePrivate(Path.Combine(components, "secrets.json"), "{}"u8.ToArray());
        WritePrivate(Path.Combine(components, "pubsub.yaml"), System.Text.Encoding.UTF8.GetBytes(
            mutation == "pubsub-db" ? component.Replace("\"5\"", "\"6\"", StringComparison.Ordinal) : component));
        if (mutation == "duplicate-db")
            WritePrivate(Path.Combine(components, "statestore.yaml"), System.Text.Encoding.UTF8.GetBytes(component + "    - name: redisDB\n      value: \"5\"\n"));
        if (mutation == "authority-db")
            WritePrivate(Path.Combine(custody, "authority.backend.json"), JsonSerializer.SerializeToUtf8Bytes(new
            {
                spec = new { metadata = new[] { new { name = "redisDB", value = "6" }, new { name = "redisHost", value = "localhost:6379" } } },
            }));
        var hashes = new Dictionary<string, string>
        {
            ["statestore.yaml"] = Hash(File.ReadAllBytes(Path.Combine(components, "statestore.yaml"))),
            ["secrets.json"] = Hash(File.ReadAllBytes(Path.Combine(components, "secrets.json"))),
            ["pubsub.yaml"] = Hash(File.ReadAllBytes(Path.Combine(components, "pubsub.yaml"))),
        };
        string receiptPath = Path.Combine(root, "provisioning.json");
        WritePrivate(receiptPath, JsonSerializer.SerializeToUtf8Bytes(new
        {
            schema = "tenants.catalog-profile.v1", identity, database = mutation == "substituted-db" ? 6 : 5,
            endpoint = "127.0.0.1:6379", privateDirectory, initializationReceipt = initializationPath,
            initializationSha256 = Hash(File.ReadAllBytes(initializationPath)), sdkSource = new string('b', 40),
            sdkProducerVersion = "5.1.1", sdkPackageProducer = new { version = "5.1.1", revision = new string('f', 40) },
            components = hashes, materialProvisionedWithFreshBackend = true,
            backend = new { initialDatabaseSize = 0, containerId = new string('a', 64), created = "2026-11-16T01:00:00Z", redisRunId = new string('e', 40) },
        }));
        var settings = new Dictionary<string, string>
        {
            ["RUNNER_TEMP"] = root, ["GITHUB_REPOSITORY"] = identity["repository"], ["GITHUB_RUN_ID"] = "42",
            ["GITHUB_RUN_ATTEMPT"] = "2", ["GITHUB_JOB"] = "ci", ["TENANTS_SOURCE_CATALOG_PROFILE"] = "true",
            ["TENANTS_SOURCE_CATALOG_REDIS_DATABASE"] = mutation == "reserved-db" ? "0" : mutation == "noncanonical-db" ? "05" : "5",
            ["TENANTS_SOURCE_CATALOG_REDIS_ENDPOINT"] = "127.0.0.1:6379", ["TENANTS_SOURCE_CATALOG_PROVISIONING_RECEIPT"] = receiptPath,
            ["TENANTS_SOURCE_CATALOG_PROVISIONING_SHA256"] = Hash(File.ReadAllBytes(receiptPath)),
            ["HEXALITH_DAPR_INITIALIZATION_RECEIPT"] = initializationPath, ["TENANTS_SOURCE_SDK_SHA"] = new string('b', 40),
            ["TENANTS_SOURCE_SDK_PRODUCER_VERSION"] = "5.1.1", ["TENANTS_SOURCE_SDK_PRODUCER_REVISION"] = new string(mutation == "producer-drift" ? 'd' : 'f', 40),
            ["Dapr__StateStoreComponentPath"] = Path.Combine(components, "statestore.yaml"),
            ["Dapr__PubSubComponentPath"] = Path.Combine(components, "pubsub.yaml"),
            ["Tenants__DevelopmentRouteCatalog__CustodyDirectory"] = custody,
        };
        if (mutation == "changed-component") File.AppendAllText(Path.Combine(components, "statestore.yaml"), "changed");
        if (mutation == "changed-initialization") File.AppendAllText(initializationPath, " ");
        if (mutation == "receipt-link")
        {
            string retained = Path.Combine(root, "retained.json");
            File.Move(receiptPath, retained);
            File.CreateSymbolicLink(receiptPath, retained);
        }
        var previous = settings.Keys.ToDictionary(key => key, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var entry in settings) Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            assertion();
        }
        finally
        {
            foreach (var entry in previous) Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void WritePrivate(string path, byte[] bytes)
    {
        File.WriteAllBytes(path, bytes);
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}

using System.Text.Json;

namespace Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality;

/// <summary>Writes only bounded diagnostic measurements outside the disposable workspace.</summary>
internal static class PrivateHashObservationOutput
{
    internal static void Write(string name, object value)
    {
        string directory = Environment.GetEnvironmentVariable("FC_PRIVATE_HASH_OBSERVATIONS")
            ?? throw new InvalidOperationException("A private observation output directory is required.");
        File.WriteAllText(Path.Combine(directory, name + ".json"), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
    }
}

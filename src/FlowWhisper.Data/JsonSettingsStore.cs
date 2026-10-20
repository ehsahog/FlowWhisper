using System.Text.Json;
using FlowWhisper.Core.Settings;

namespace FlowWhisper.Data;

/// <summary>
/// Persists AppSettings as a single JSON file. A single small config object
/// doesn't need SQLite's query/concurrency machinery — a file is simpler and
/// just as durable for this.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly string _path;

    public JsonSettingsStore(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }

        try
        {
            using var stream = File.OpenRead(_path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream).ConfigureAwait(false);
            return settings ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupt settings file; fall back to defaults rather than crash the app.
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings)
    {
        using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true })
            .ConfigureAwait(false);
    }
}

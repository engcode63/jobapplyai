using System.Text.Json;

namespace JobApplyAI.Mobile.Services;

/// <summary>
/// Lightweight on-device JSON cache so the last-loaded Resumes/Applications/Job Postings/Cover
/// Letters remain viewable when the Functions API is unreachable (e.g. no signal, API host down).
/// Not a sync engine - writes made offline are not queued/replayed, this only keeps the last known
/// server state visible instead of an empty error screen.
/// </summary>
public static class OfflineCache
{
    private static string PathFor(string key) => Path.Combine(FileSystem.Current.AppDataDirectory, $"cache-{key}.json");

    public static async Task SaveAsync<T>(string key, T data)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(PathFor(key), json);
        }
        catch
        {
            // Caching is a nice-to-have - never let a cache write failure break the UI flow.
        }
    }

    public static async Task<T?> LoadAsync<T>(string key)
    {
        try
        {
            var path = PathFor(key);
            if (!File.Exists(path)) return default;
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }
}

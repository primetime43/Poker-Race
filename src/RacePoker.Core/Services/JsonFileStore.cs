using System.Text.Json;

namespace RacePoker.Core.Services;

/// <summary>Atomic writes with a previous-version backup and nonfatal recovery.</summary>
internal static class JsonFileStore
{
    internal static T Load<T>(string path, Func<T> defaults, out string? notice)
    {
        notice = null;
        if (!File.Exists(path)) return defaults();
        try { return Read<T>(path); }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // Preserve the damaged original before a later save can replace it.
            try { File.Copy(path, path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")); }
            catch (Exception backupError) when (backupError is IOException or UnauthorizedAccessException) { }
            try
            {
                var recovered = Read<T>(path + ".bak");
                notice = "Recovered saved data from its previous backup.";
                return recovered;
            }
            catch (Exception backupError) when (backupError is JsonException or IOException or UnauthorizedAccessException)
            {
                notice = "Saved data could not be read. Defaults are in use; the original file has been retained.";
                return defaults();
            }
        }
    }

    private static T Read<T>(string path)
    {
        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json)) throw new JsonException("Empty data file.");
        return JsonSerializer.Deserialize<T>(json) ?? throw new JsonException("Null data file.");
    }

    internal static bool Save<T>(string path, T value, out string? notice)
    {
        notice = null;
        var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            var json = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions { WriteIndented = true });
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(json);
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else File.Move(temp, path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            notice = "Changes are available for this session, but could not be saved. Check the data folder's access and free space.";
            return false;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}

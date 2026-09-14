namespace Zytadelle.Core.Persistence;

/// <summary>
/// Where the save lives. Kept behind an interface so the simulation stays testable and the app
/// decides whether that is a file, an app-data folder or nothing at all.
/// </summary>
public interface ISaveStorage
{
    string? Read();

    void Write(string json);
}

/// <summary>A save that is never persisted. Useful for probes and for a storage-less environment.</summary>
public sealed class MemorySaveStorage : ISaveStorage
{
    private string? _json;

    public string? Read() => _json;

    public void Write(string json) => _json = json;
}

/// <summary>A save in a single JSON file. Failures are swallowed: losing a save beats crashing.</summary>
public sealed class FileSaveStorage(string path) : ISaveStorage
{
    public string Path { get; } = path;

    public string? Read()
    {
        try
        {
            return File.Exists(Path) ? File.ReadAllText(Path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Write(string json)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(Path, json);
        }
        catch (IOException)
        {
            // storage unavailable - ignore, same as the browser build did
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

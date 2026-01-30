namespace Celeste.Core.Services;

/// <summary>
/// Implementação de IPlatformPaths para desktop/desenvolvimento.
/// </summary>
public class DesktopPlatformPaths : IPlatformPaths
{
    private readonly string _basePath;

    public string ContentRoot => Path.Combine(_basePath, "Content");
    public string SavesRoot => Path.Combine(_basePath, "Saves");
    public string LogsRoot => Path.Combine(_basePath, "Logs");
    public string TempRoot => Path.Combine(_basePath, "Temp");

    public DesktopPlatformPaths(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CelesteAndroid");
    }

    public string ResolveContentPath(string relativePath)
    {
        return Path.Combine(ContentRoot, relativePath);
    }

    public string ResolveSavePath(string relativePath)
    {
        return Path.Combine(SavesRoot, relativePath);
    }

    public string ResolveLogPath(string filename)
    {
        return Path.Combine(LogsRoot, filename);
    }

    public void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(ContentRoot);
        Directory.CreateDirectory(SavesRoot);
        Directory.CreateDirectory(LogsRoot);
        Directory.CreateDirectory(TempRoot);
    }
}

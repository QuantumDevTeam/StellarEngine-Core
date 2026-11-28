namespace Stellar.Tools.Tests;

/// <summary>
/// A directory that lives for the duration of one test.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            $"stellar-tests-{Guid.NewGuid():N}")).FullName;

    public string Write(string relativePath, string content = "content")
    {
        string path = System.IO.Path.Combine(Path, relativePath);

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);

        return path;
    }

    public void Dispose()
    {
        // git makes its objects read-only, and Windows does not delete those
        foreach (FileInfo file in new DirectoryInfo(Path).EnumerateFiles("*", SearchOption.AllDirectories))
            if (file.IsReadOnly)
                file.IsReadOnly = false;

        Directory.Delete(Path, true);
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Stellar.Tools.Diagnostics;

namespace Stellar.Tools.Marks;

/// <summary>
/// The marks of a git repository, and the hooks that check them.
/// </summary>
public sealed class MarkRepository
{
    private const string HookMark = "# stellar marks";
    private static readonly string[] HookNames = ["pre-commit", "pre-merge-commit", "pre-push"];

    private MarkRepository(string root)
    {
        Root = root;
    }

    /// <summary>
    /// Gets the folder of the repository.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Opens the repository a directory lies in.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <returns>The repository.</returns>
    /// <exception cref="ToolException">Thrown if there is none.</exception>
    public static MarkRepository Open(string directory)
    {
        return new MarkRepository(Path.GetFullPath(Git(directory, "rev-parse", "--show-toplevel").Trim()));
    }

    /// <summary>
    /// Opens the core of an installation and every module and tool that is a repository.
    /// </summary>
    /// <param name="root">The root of the installation.</param>
    /// <returns>The repositories, the core first.</returns>
    public static IReadOnlyList<MarkRepository> OpenAll(string root)
    {
        List<MarkRepository> repositories = [Open(root)];

        foreach (string area in new[] { "modules", "tools" }.Select(area => Path.Combine(root, area))
                     .Where(Directory.Exists))
        foreach (string directory in Directory.GetDirectories(area).OrderBy(path => path, StringComparer.Ordinal))
            // A file, not a folder, in a worktree
            if (Directory.Exists(Path.Combine(directory, ".git")) || File.Exists(Path.Combine(directory, ".git")))
                repositories.Add(Open(directory));

        return repositories;
    }

    /// <summary>
    /// Finds the marks of the files in the folder, the ignored ones left out.
    /// </summary>
    /// <returns>The marks.</returns>
    public IReadOnlyList<Mark> ScanFiles()
    {
        return Split(Git(Root, "ls-files", "-z", "--cached", "--others", "--exclude-standard"))
            .Where(MarkScanner.Knows)
            .Distinct()
            .Where(file => File.Exists(Path.Combine(Root, file)))
            .SelectMany(file => MarkScanner.Scan(file, File.ReadAllText(Path.Combine(Root, file))))
            .ToList();
    }

    /// <summary>
    /// Finds the marks of what the next commit changes, as it will be committed.
    /// </summary>
    /// <returns>The marks, the old ones of the changed files too.</returns>
    public IReadOnlyList<Mark> ScanStaged()
    {
        return Split(Git(Root, "diff", "--cached", "--name-only", "-z", "--diff-filter=ACMRT", "--no-ext-diff"))
            .Where(MarkScanner.Knows)
            .SelectMany(file => MarkScanner.Scan(file, Git(Root, "show", ":" + file)))
            .ToList();
    }

    /// <summary>
    /// Finds the marks of a commit.
    /// </summary>
    /// <param name="revision">The commit.</param>
    /// <returns>The marks.</returns>
    public IReadOnlyList<Mark> ScanRevision(string revision)
    {
        string commit = Git(Root, "rev-parse", "--verify", "--end-of-options", revision + "^{commit}").Trim();
        return Split(Git(Root, "ls-tree", "-r", "-z", "--name-only", "--full-tree", commit))
            .Where(MarkScanner.Knows)
            .Where(new HashSet<string>(FilesWithMarks(commit), StringComparer.Ordinal).Contains)
            .SelectMany(file => MarkScanner.Scan(file, Git(Root, "show", commit + ":" + file)))
            .ToList();
    }

    /// <summary>
    /// Puts in the hooks that check the marks of every commit, merge and push.
    /// </summary>
    /// <returns>The hooks written.</returns>
    /// <exception cref="ToolException">Thrown if the repository has a hook of somebody else: it is left alone.</exception>
    public IReadOnlyList<string> InstallHooks()
    {
        string hooks = HooksDirectory();
        Dictionary<string, string> paths = HookNames.ToDictionary(name => name, name => Path.Combine(hooks, name));

        string? foreign = paths.Values.FirstOrDefault(path => File.Exists(path) && !IsOurs(path));
        if (foreign is not null)
            throw new ToolException(
                $"{foreign} is not a hook of 'stellar marks': add 'stellar marks check' to it by hand");

        Directory.CreateDirectory(hooks);
        foreach (KeyValuePair<string, string> pair in paths)
        {
            string what = pair.Key == "pre-push" ? "--revision HEAD" : "--staged";
            // A command that is missing or fails stops the commit too: nothing goes unchecked by accident
            File.WriteAllText(pair.Value,
                $"#!/bin/sh\n{HookMark}\nstellar marks check {what} || {{\n  echo \"Stopped by the marks of the code (stellar marks check {what})\" >&2\n  exit 1\n}}\n");
#if NET
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(pair.Value,
                    File.GetUnixFileMode(pair.Value) | UnixFileMode.UserExecute | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherExecute);
#endif
        }

        return paths.Values.ToList();
    }

    /// <summary>
    /// Takes the hooks out, only those <see cref="InstallHooks"/> made.
    /// </summary>
    /// <returns>The hooks removed.</returns>
    public IReadOnlyList<string> RemoveHooks()
    {
        string hooks = HooksDirectory();
        List<string> removed = HookNames.Select(name => Path.Combine(hooks, name))
            .Where(path => File.Exists(path) && IsOurs(path)).ToList();
        removed.ForEach(File.Delete);
        return removed;
    }

    private static bool IsOurs(string path)
    {
        return File.ReadAllText(path).Contains(HookMark);
    }

    private string HooksDirectory()
    {
        return Path.GetFullPath(Path.Combine(Root, Git(Root, "rev-parse", "--git-path", "hooks").Trim()));
    }

    // git grep ends with 1 when it finds nothing
    private IEnumerable<string> FilesWithMarks(string commit)
    {
        string prefix = commit + ":";
        return Split(Git(Root, [0, 1], "grep", "-l", "-z", "-I", "-F", "--full-name", "-e", "#<", commit))
            .Select(entry =>
                entry.StartsWith(prefix, StringComparison.Ordinal) ? entry.Substring(prefix.Length) : entry);
    }

    private static IEnumerable<string> Split(string output)
    {
        return output.Split(['\0'], StringSplitOptions.RemoveEmptyEntries);
    }

    private static string Git(string directory, params string[] arguments)
    {
        return Git(directory, [0], arguments);
    }

    private static string Git(string directory, int[] successes, params string[] arguments)
    {
        var start = new ProcessStartInfo("git", string.Join(" ", arguments.Select(Quote)))
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };

        Process process;
        try
        {
            process = Process.Start(start)!;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            throw new ToolException("git was not found", exception);
        }

        using (process)
        {
            Task<string> error = Task.Run(() => process.StandardError.ReadToEnd());
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return successes.Contains(process.ExitCode)
                ? output
                : throw new ToolException(
                    $"git {string.Join(" ", arguments)} in {directory} failed: {error.Result.Trim()}");
        }
    }

    private static string Quote(string argument)
    {
        return "\"" + argument.Replace("\"", "\\\"") + "\"";
    }
}

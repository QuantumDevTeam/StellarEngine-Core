using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;

namespace Stellar.Tools.Marks;

/// <summary>
/// How far code with a mark may go.
/// </summary>
public enum MarkLevel
{
    /// <summary>Anywhere: the mark only tells something.</summary>
    Info,

    /// <summary>Into commits, but not into a release.</summary>
    Release,

    /// <summary>Nowhere: not even into a commit.</summary>
    Commit
}

/// <summary>
/// What is about to happen to the code.
/// </summary>
public enum MarkStage
{
    /// <summary>A commit or a push.</summary>
    Commit,

    /// <summary>A release, or a snapshot of the mirror.</summary>
    Release
}

/// <summary>
/// A kind of marks.
/// </summary>
public sealed class MarkKind
{
    /// <summary>
    /// Gets or sets how far code with such a mark may go.
    /// </summary>
    public MarkLevel Level { get; set; }

    /// <summary>
    /// Gets or sets what the mark means.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// The kinds of marks: those of the engine (<c>Shared/Marks/marks.json</c>) and those of the machine
/// (<c>config/marks.json</c>, never committed), which win.
/// </summary>
/// <remarks>A mark of a kind nobody knows is stopped at every stage: a typing error, or a kind of another machine.</remarks>
public sealed class MarkCatalog
{
    /// <summary>
    /// The file of the kinds, in <c>Shared/Marks</c> and in <c>config</c>.
    /// </summary>
    public const string FileName = "marks.json";

    private static readonly Regex Name = new("^[A-Z][A-Z0-9_-]*$", RegexOptions.CultureInvariant);

    private readonly Dictionary<string, MarkKind> _kinds = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the kinds, by name.
    /// </summary>
    public IReadOnlyDictionary<string, MarkKind> Kinds => _kinds;

    /// <summary>
    /// Reads the kinds of an installation.
    /// </summary>
    /// <param name="root">The root of the installation.</param>
    /// <returns>The kinds.</returns>
    /// <exception cref="ToolException">Thrown if there is no file of the engine, or a file is malformed.</exception>
    public static MarkCatalog Load(string root)
    {
        var catalog = new MarkCatalog();
        catalog.Add(Path.Combine(root, "Shared", "Marks", FileName));

        string own = Path.Combine(root, "config", FileName);
        if (File.Exists(own)) catalog.Add(own);
        return catalog;
    }

    /// <summary>
    /// Finds the root of the installation a directory lies in.
    /// </summary>
    /// <param name="directory">The directory.</param>
    /// <returns>The root, or <c>null</c>.</returns>
    public static string? FindRoot(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
            if (File.Exists(Path.Combine(current.FullName, "Shared", "Marks", FileName)))
                return current.FullName;

        return null;
    }

    /// <summary>
    /// Whether a mark stops the code.
    /// </summary>
    /// <param name="name">The name of the mark.</param>
    /// <param name="stage">What is about to happen.</param>
    /// <returns><c>true</c> if the code must not go on.</returns>
    public bool Stops(string name, MarkStage stage)
    {
        if (!_kinds.TryGetValue(name, out MarkKind? kind)) return true;
        return kind.Level == MarkLevel.Commit || (kind.Level == MarkLevel.Release && stage == MarkStage.Release);
    }

    private void Add(string path)
    {
        foreach (KeyValuePair<string, MarkKind> pair in StellarJson.Read<MarkFile>(path).Marks)
        {
            if (!Name.IsMatch(pair.Key))
                throw new ToolException(
                    $"'{pair.Key}' in {path} is not the name of a mark: capital letters, digits, '-' and '_'");
            _kinds[pair.Key] = pair.Value;
        }
    }

    private sealed class MarkFile
    {
        public Dictionary<string, MarkKind> Marks { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stellar.Tools.Marks;

/// <summary>
/// A mark found in a file.
/// </summary>
/// <param name="File">The file.</param>
/// <param name="Line">The line, from 1.</param>
/// <param name="Name">The name of the mark.</param>
/// <param name="Text">The line it stands in.</param>
public sealed record Mark(string File, int Line, string Name, string Text);

/// <summary>
/// Finds marks in the comments of sources: <c>#&lt;NAME&gt;</c>, capital letters, digits, '-' and '_'.
/// </summary>
/// <remarks>
/// It knows comments, not languages: what looks like the start of a comment inside a string counts as one, so a mark
/// may be found where there is none, but is never missed. Files of other kinds have no comments and no marks.
/// </remarks>
public static class MarkScanner
{
    private sealed class Style
    {
        public Style(string? line, string? open, string? close, params string[] extensions)
        {
            Line = line;
            Open = open;
            Close = close;
            Extensions = extensions;
        }

        public string? Line { get; }
        public string? Open { get; }
        public string? Close { get; }
        public string[] Extensions { get; }
    }

    private static readonly Regex Pattern = new("#<([A-Z][A-Z0-9_-]*)>", RegexOptions.CultureInvariant);

    private static readonly Style[] Styles =
    {
        new("//", "/*", "*/", ".cs", ".c", ".cc", ".cpp", ".cxx", ".h", ".hh", ".hpp", ".hxx", ".inl", ".ixx", ".hlsl",
            ".hlsli", ".glsl", ".vert", ".frag", ".comp", ".js", ".ts", ".css", ".rc"),
        new("#", "<#", "#>", ".ps1", ".psm1", ".psd1"),
        new("#", null, null, ".yml", ".yaml", ".sh", ".py", ".toml", ".editorconfig", ".gitignore", ".gitattributes"),
        new(null, "<!--", "-->", ".xml", ".csproj", ".vcxproj", ".props", ".targets", ".filters", ".config", ".md",
            ".html", ".xaml", ".axaml", ".svg", ".resx")
    };

    private static readonly Dictionary<string, Style> ByExtension =
        Styles.SelectMany(style => style.Extensions.Select(extension => (extension, style)))
            .ToDictionary(pair => pair.extension, pair => pair.style, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a file may hold marks.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns><c>true</c> if its comments are known.</returns>
    public static bool Knows(string file)
    {
        return ByExtension.ContainsKey(Path.GetExtension(file));
    }

    /// <summary>
    /// Finds the marks of a text.
    /// </summary>
    /// <param name="file">The file the text is of: its extension tells the comments.</param>
    /// <param name="text">The text.</param>
    /// <returns>The marks, in order.</returns>
    public static IReadOnlyList<Mark> Scan(string file, string text)
    {
        List<Mark> marks = new();
        if (!ByExtension.TryGetValue(Path.GetExtension(file), out Style? style)) return marks;

        bool inBlock = false;
        int number = 0;
        foreach (string line in text.Split('\n'))
        {
            number++;
            // The comments of the line, as ranges: a mark counts if it starts in one
            List<(int Start, int End)> ranges = Comments(line, style, ref inBlock);
            foreach (Match match in Pattern.Matches(line))
                if (ranges.Any(range => match.Index >= range.Start && match.Index < range.End))
                    marks.Add(new Mark(file, number, match.Groups[1].Value, line.Trim()));
        }

        return marks;
    }

    private static List<(int Start, int End)> Comments(string line, Style style, ref bool inBlock)
    {
        List<(int Start, int End)> ranges = new();
        int position = 0;

        while (position < line.Length)
        {
            if (inBlock)
            {
                int close = line.IndexOf(style.Close!, position, StringComparison.Ordinal);
                ranges.Add((position, close < 0 ? line.Length : close));
                if (close < 0) return ranges;
                inBlock = false;
                position = close + style.Close!.Length;
                continue;
            }

            int open = style.Open is null ? -1 : line.IndexOf(style.Open, position, StringComparison.Ordinal);
            int single = style.Line is null ? -1 : line.IndexOf(style.Line, position, StringComparison.Ordinal);

            if (single >= 0 && (open < 0 || single < open))
            {
                ranges.Add((single, line.Length));
                return ranges;
            }

            if (open < 0) return ranges;
            inBlock = true;
            position = open + style.Open!.Length;
        }

        return ranges;
    }
}

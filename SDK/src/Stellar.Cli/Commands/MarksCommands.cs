using System.ComponentModel;

using Spectre.Console;
using Spectre.Console.Cli;

using Stellar.Tools.Diagnostics;
using Stellar.Tools.Environment;
using Stellar.Tools.Marks;

namespace Stellar.Cli.Commands;

/// <summary>
/// What every <c>stellar marks</c> command takes.
/// </summary>
public class MarksSettings
    : CommandSettings
{
    [CommandArgument(0, "[PATH]")]
    [Description("A folder of the repository. The current one when omitted")]
    public string? Path { get; set; }

    [CommandOption("-a|--all")]
    [Description("The core and every module and tool of the installation, not one repository")]
    public bool All { get; set; }

    internal string Directory => System.IO.Path.GetFullPath(Path ?? Environment.CurrentDirectory);

    // The kinds lie in the core, above the repository or in the registered installation
    internal string Root => MarkCatalog.FindRoot(Directory)
                            ?? StellarInstallation.TryLocate(Directory, InstallationRegistry.ForCurrentUser())?.Root
                            ?? throw new ToolException(
                                $"No installation for {Directory}: the kinds of marks are not known");

    internal IReadOnlyList<MarkRepository> Repositories =>
        All ? MarkRepository.OpenAll(Root) : [MarkRepository.Open(Directory)];
}

/// <summary>
/// The settings of <c>stellar marks check</c>.
/// </summary>
public sealed class CheckMarksSettings
    : MarksSettings
{
    [CommandOption("--staged")]
    [Description("What the next commit changes, as it will be committed")]
    public bool Staged { get; set; }

    [CommandOption("--revision <REVISION>")]
    [Description("A commit, not the files of the folder")]
    public string? Revision { get; set; }

    [CommandOption("--stage <STAGE>")]
    [Description("What is about to happen: commit (the default) or release")]
    [DefaultValue(MarkStage.Commit)]
    public MarkStage Stage { get; set; }

    public override ValidationResult Validate()
    {
        return Staged && Revision is not null
            ? ValidationResult.Error("--staged and --revision exclude one another")
            : ValidationResult.Success();
    }
}

/// <summary>
/// <c>stellar marks check</c>: fails if a mark stops the code at the stage.
/// </summary>
public sealed class CheckMarksCommand
    : Command<CheckMarksSettings>
{
    public override int Execute(CommandContext context, CheckMarksSettings settings)
    {
        MarkCatalog catalog = MarkCatalog.Load(settings.Root);
        int stopped = 0;
        int total = 0;

        foreach (MarkRepository repository in settings.Repositories)
        {
            IReadOnlyList<Mark> marks = settings.Staged ? repository.ScanStaged()
                : settings.Revision is { } revision ? repository.ScanRevision(revision)
                : repository.ScanFiles();
            total += marks.Count;

            foreach (Mark mark in marks.Where(mark => catalog.Stops(mark.Name, settings.Stage)))
            {
                stopped++;
                string why = catalog.Kinds.TryGetValue(mark.Name, out MarkKind? kind)
                    ? kind.Description
                    : "unknown mark";
                Console.Error.WriteLine($"{Path.Combine(repository.Root, mark.File)}:{mark.Line}: {mark.Name}: {why}");
            }
        }

        if (stopped > 0)
        {
            Console.Error.WriteLine($"{stopped} mark(s) stop the {settings.Stage.ToString().ToLowerInvariant()}");
            return 1;
        }

        Console.WriteLine($"Marks: {total}, none stops the {settings.Stage.ToString().ToLowerInvariant()}");
        return 0;
    }
}

/// <summary>
/// <c>stellar marks list</c>: the marks and what they mean.
/// </summary>
public sealed class ListMarksCommand
    : Command<MarksSettings>
{
    public override int Execute(CommandContext context, MarksSettings settings)
    {
        MarkCatalog catalog = MarkCatalog.Load(settings.Root);
        ILookup<string, string> marks = settings.Repositories
            .SelectMany(repository => repository.ScanFiles()
                .Select(mark => (mark.Name, Place: $"{Path.Combine(repository.Root, mark.File)}:{mark.Line}")))
            .ToLookup(mark => mark.Name, mark => mark.Place);

        foreach (string name in catalog.Kinds.Keys.Union(marks.Select(group => group.Key))
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            string about = catalog.Kinds.TryGetValue(name, out MarkKind? kind)
                ? $"{kind.Level}: {kind.Description}"
                : "unknown";
            Console.WriteLine($"{name} ({marks[name].Count()}) {about}");
            foreach (string place in marks[name]) Console.WriteLine($"  {place}");
        }

        return 0;
    }
}

/// <summary>
/// <c>stellar marks install</c>: git checks the marks of every commit, merge and push.
/// </summary>
public sealed class InstallMarksCommand
    : Command<MarksSettings>
{
    public override int Execute(CommandContext context, MarksSettings settings)
    {
        MarkCatalog.Load(settings.Root);
        foreach (MarkRepository repository in settings.Repositories)
        {
            repository.InstallHooks();
            Console.WriteLine($"Checks the marks: {repository.Root}");
        }

        return 0;
    }
}

/// <summary>
/// <c>stellar marks uninstall</c>: takes the checks out of git.
/// </summary>
public sealed class UninstallMarksCommand
    : Command<MarksSettings>
{
    public override int Execute(CommandContext context, MarksSettings settings)
    {
        foreach (MarkRepository repository in settings.Repositories)
            Console.WriteLine($"Removed {repository.RemoveHooks().Count} hook(s): {repository.Root}");

        return 0;
    }
}

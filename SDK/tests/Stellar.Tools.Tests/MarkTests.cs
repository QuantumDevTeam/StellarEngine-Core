using System.Diagnostics;

using Stellar.Tools.Diagnostics;
using Stellar.Tools.Marks;

namespace Stellar.Tools.Tests;

public class MarkTests
{
    // Put together here, or this file would carry the marks itself
    private static string M(string name)
    {
        return "#" + "<" + name + ">";
    }

    private static string[] Names(string file, string text)
    {
        return MarkScanner.Scan(file, text).Select(mark => $"{mark.Line}:{mark.Name}").ToArray();
    }

    [Fact]
    public void Scan_FindsMarksInEveryKindOfComment()
    {
        Names("a.cs", $"int a; // {M("MVP")}\n/* {M("A1")}\n * {M("B-2")} */ int b;\n/// <x/> {M("C_3")}")
            .Should().Equal("1:MVP", "2:A1", "3:B-2", "4:C_3");
        Names("a.h", $"/* x */ /* {M("MVP")} */").Should().Equal("1:MVP");
        Names("a.ps1", $"{M("MVP")}\n<#\n {M("WIP")}\n#>").Should().Equal("1:MVP", "3:WIP");
        Names("a.yml", $"a: 1 # {M("MVP")}").Should().Equal("1:MVP");
        Names("a.props", $"<P>\n<!-- {M("MVP")}\n{M("WIP")} -->").Should().Equal("2:MVP", "3:WIP");
        Names("A.CS", $"// {M("MVP")}\r\n// {M("WIP")}\r\n").Should().Equal("1:MVP", "2:WIP");
        Names(".gitignore", M("MVP")).Should().Equal("1:MVP");
        Names("a.cs", $"// {M("MVP")} {M("WIP")}").Should().Equal("1:MVP", "1:WIP");
    }

    [Fact]
    public void Scan_LeavesCodeAndOtherFilesAlone()
    {
        Names("a.cs", $"var a = \"{M("MVP")}\";").Should().BeEmpty();
        Names("a.cs", $"/* x */ {M("MVP")}").Should().BeEmpty();
        Names("a.props", $"<P>{M("MVP")}</P>").Should().BeEmpty();
        Names("a.md", $"text {M("MVP")}").Should().BeEmpty();
        Names("a.json", $"// {M("MVP")}").Should().BeEmpty();
        Names("a.cs", $"// {M("mvp")} {M("1A")} {M("")} #< MVP> List<MVP>").Should().BeEmpty();
    }

    private static MarkCatalog Catalog(TempDirectory directory, string? own = null)
    {
        directory.Write("Shared/Marks/marks.json",
            """{ "Marks": { "MVP": { "Level": "Info" }, "WIP": { "Level": "Release" }, "STOP": { "Level": "Commit" } } }""");
        if (own is not null) directory.Write("config/marks.json", own);
        return MarkCatalog.Load(directory.Path);
    }

    [Theory]
    [InlineData("MVP", false, false)]
    [InlineData("WIP", false, true)]
    [InlineData("STOP", true, true)]
    [InlineData("UNKNOWN", true, true)]
    public void Levels_StopAtTheirStages(string name, bool commit, bool release)
    {
        using var directory = new TempDirectory();
        MarkCatalog catalog = Catalog(directory);

        catalog.Stops(name, MarkStage.Commit).Should().Be(commit);
        catalog.Stops(name, MarkStage.Release).Should().Be(release);
    }

    [Fact]
    public void KindsOfTheMachine_AddAndReplace()
    {
        using var directory = new TempDirectory();
        MarkCatalog catalog = Catalog(directory,
            """{ "Marks": { "OWN": { "Level": "Commit" }, "WIP": { "Level": "Info" } } }""");

        catalog.Kinds.Keys.Should().BeEquivalentTo("MVP", "WIP", "STOP", "OWN");
        catalog.Stops("WIP", MarkStage.Release).Should().BeFalse();
        catalog.Stops("OWN", MarkStage.Commit).Should().BeTrue();
    }

    [Fact]
    public void ABadCatalog_IsAnError_NeverAnEmptyOne()
    {
        using var directory = new TempDirectory();

        FluentActions.Invoking(() => MarkCatalog.Load(directory.Path)).Should().Throw<ToolException>();
        FluentActions.Invoking(() => Catalog(directory, """{ "Marks": { "own": {} } }""")).Should()
            .Throw<ToolException>();
        FluentActions.Invoking(() => Catalog(directory, """{ "Marks": { "OWN": { "Level": "Nowhere" } } }"""))
            .Should().Throw<ToolException>();
    }

    [Fact]
    public void FindRoot_GoesUp()
    {
        using var directory = new TempDirectory();
        Catalog(directory);
        directory.Write("modules/Stellar.A/src/a.cs", "");

        MarkCatalog.FindRoot(Path.Combine(directory.Path, "modules", "Stellar.A", "src")).Should().Be(directory.Path);
        MarkCatalog.FindRoot(Path.GetTempPath()).Should().BeNull();
    }

    private static string Git(string directory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
            { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in (string[])
                 [
                     "-c", "user.name=Tests", "-c", "user.email=tests@localhost", "-c", "commit.gpgsign=false", "-c",
                     "core.hooksPath=.none", .. arguments
                 ])
            start.ArgumentList.Add(argument);
        using Process process = Process.Start(start)!;
        Task<string> error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? output : throw new InvalidOperationException(error.Result);
    }

    [Fact]
    public void Repository_TellsStagedFromTheFolderAndFromACommit()
    {
        using var directory = new TempDirectory();
        Git(directory.Path, "init", "-q");
        directory.Write(".gitignore", "/hidden/\n");
        directory.Write("папка с пробелом/a.cs", $"// {M("MVP")}\n");
        directory.Write("hidden/b.cs", $"// {M("OWN")}\n");
        Git(directory.Path, "add", ".");
        Git(directory.Path, "commit", "-q", "-m", "one");

        directory.Write("папка с пробелом/a.cs", $"// {M("WIP")}\n");
        Git(directory.Path, "add", ".");
        directory.Write("папка с пробелом/a.cs", $"// {M("STOP")}\n");
        directory.Write("new.cs", $"// {M("NEW")}\n");

        MarkRepository repository = MarkRepository.Open(Path.Combine(directory.Path, "папка с пробелом"));
        repository.ScanRevision("HEAD").Select(mark => mark.Name).Should().Equal("MVP");
        repository.ScanStaged().Select(mark => mark.Name).Should().Equal("WIP");
        repository.ScanFiles().Select(mark => mark.Name).Should().BeEquivalentTo("STOP", "NEW");
        repository.ScanRevision("HEAD").Single().File.Should().Be("папка с пробелом/a.cs");
        repository.Invoking(it => it.ScanRevision("nowhere")).Should().Throw<ToolException>();
    }

    [Fact]
    public void Repository_WithoutMarks_HasNone_AndACommitThatDeletes_IsFine()
    {
        using var directory = new TempDirectory();
        Git(directory.Path, "init", "-q");
        directory.Write("a.cs", "int a;\n");
        Git(directory.Path, "add", ".");
        Git(directory.Path, "commit", "-q", "-m", "one");
        Git(directory.Path, "rm", "-q", "a.cs");

        MarkRepository repository = MarkRepository.Open(directory.Path);
        repository.ScanRevision("HEAD").Should().BeEmpty();
        repository.ScanStaged().Should().BeEmpty();
        repository.ScanFiles().Should().BeEmpty();
    }

    [Fact]
    public void Hooks_AreOurs_OrLeftAlone()
    {
        using var directory = new TempDirectory();
        Git(directory.Path, "init", "-q");
        MarkRepository repository = MarkRepository.Open(directory.Path);

        repository.InstallHooks().Should().HaveCount(3);
        repository.InstallHooks().Should().HaveCount(3);
        repository.RemoveHooks().Should().HaveCount(3);
        repository.RemoveHooks().Should().BeEmpty();

        directory.Write(".git/hooks/pre-commit", "#!/bin/sh\nexit 0\n");
        repository.Invoking(it => it.InstallHooks()).Should().Throw<ToolException>();
        repository.RemoveHooks().Should().BeEmpty();
        File.Exists(Path.Combine(directory.Path, ".git", "hooks", "pre-commit")).Should().BeTrue();
        File.Exists(Path.Combine(directory.Path, ".git", "hooks", "pre-push")).Should().BeFalse();
    }

    [Fact]
    public void OpenAll_TakesTheCoreAndItsComponents()
    {
        using var directory = new TempDirectory();
        Git(directory.Path, "init", "-q");
        directory.Write("modules/Stellar.A/a.cs", "");
        directory.Write("modules/NotARepository/a.cs", "");
        Git(Path.Combine(directory.Path, "modules", "Stellar.A"), "init", "-q");

        MarkRepository.OpenAll(directory.Path).Select(repository => Path.GetFileName(repository.Root))
            .Should().Equal(Path.GetFileName(directory.Path), "Stellar.A");
    }

    [Fact]
    public void NoRepository_IsAnError()
    {
        using var directory = new TempDirectory();
        FluentActions.Invoking(() => MarkRepository.Open(directory.Path)).Should().Throw<ToolException>();
    }
}

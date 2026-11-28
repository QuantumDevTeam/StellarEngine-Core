using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Stellar.Sdk.Analyzers.Configuration;
using Stellar.Sdk.Analyzers.Diagnostics;
using Stellar.Sdk.Analyzers.Internal;

namespace Stellar.Sdk.Analyzers.Analyzers.Modules;

/// <summary>
/// Keeps the architecture of the engine: the level rule between modules, and the descriptor as the single
/// description of what a module depends on.
/// </summary>
/// <remarks>
/// The level and the dependencies of the project come from the SDK (CompilerVisibleProperty), the ones of the
/// referenced assemblies from the <c>[AssemblyMetadata("StellarModule…")]</c> the SDK stamps into every module.
/// Assemblies without it are not modules, and are not checked.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleArchitectureAnalyzer
    : DiagnosticAnalyzer
{
    // ── Well-known Stellar type names ─────────────────────────────────────

    private const string MetadataAttribute = "System.Reflection.AssemblyMetadataAttribute";
    private const string DescriptorExtension = ".stellar.module";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Dictionary<string, string> ReadMetadata(IAssemblySymbol assembly)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);

        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != MetadataAttribute ||
                attribute.ConstructorArguments.Length != 2)
                continue;

            if (attribute.ConstructorArguments[0].Value is string key &&
                attribute.ConstructorArguments[1].Value is string value)
                values[key] = value;
        }

        return values;
    }

    private static void CheckDescriptor(CompilationAnalysisContext context, string id)
    {
        AdditionalText? descriptor = context.Options.AdditionalFiles.FirstOrDefault(file =>
            file.Path.EndsWith(DescriptorExtension, StringComparison.OrdinalIgnoreCase));
        SourceText? text = descriptor?.GetText(context.CancellationToken);
        if (descriptor is null || text is null)
            return;

        string? describedId = MinimalJson.GetString(text.ToString(), "Id");
        if (describedId is null || string.Equals(describedId, id, StringComparison.Ordinal))
            return;

        // ── STLR0203: module descriptor mismatch ──────────────────────────
        context.ReportDiagnostic(Diagnostic.Create(
            StellarDiagnosticDescriptors.ModuleDescriptorMismatch,
            JsonTextLocator.FindValueLocation(text, descriptor.Path, "Id"),
            describedId, id));
    }

    private static void AddDependencies(
        Dictionary<string, string> metadata,
        Dictionary<string, Dictionary<string, string>> modules,
        HashSet<string> found
    )
    {
        if (!metadata.TryGetValue("StellarModuleDependencies", out string? list))
            return;

        foreach (string dependency in list
                     .Split([';'], StringSplitOptions.RemoveEmptyEntries)
                     .Select(name => name.Trim()))
            if (found.Add(dependency) && modules.TryGetValue(dependency, out Dictionary<string, string>? next))
                AddDependencies(next, modules, found);
    }

    // ── DiagnosticAnalyzer ────────────────────────────────────────────────

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            StellarDiagnosticDescriptors.ModuleLevelViolation,
            StellarDiagnosticDescriptors.UndeclaredModuleDependency,
            StellarDiagnosticDescriptors.ModuleDescriptorMismatch);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    // ── Compilation action ────────────────────────────────────────────────

    private static void Analyze(CompilationAnalysisContext context)
    {
        AnalyzerConfigOptions options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;

        string? Property(string name)
        {
            return options.TryGetValue("build_property." + name, out string? value)
                   && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : null;
        }

        string id = Property("StellarModuleId") ?? context.Compilation.AssemblyName ?? string.Empty;
        bool isModule = string.Equals(Property("StellarIsModule"), "true", StringComparison.OrdinalIgnoreCase);
        int? level = ModuleLevels.Parse(Property("StellarModuleLevel"))
                     ?? ModuleLevels.FromUsage(Property("StellarSdkUsage"));

        if (level is null)
            return;

        HashSet<string> declared = new(
            (Property("StellarModuleDependencies") ?? string.Empty)
            .Split([';'], StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim()),
            StringComparer.Ordinal);

        // Every referenced module, with what it says about itself
        Dictionary<string, Dictionary<string, string>> modules = new(StringComparer.Ordinal);
        foreach (MetadataReference reference in context.Compilation.References)
        {
            if (context.Compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
                continue;

            Dictionary<string, string> metadata = ReadMetadata(assembly);
            if (metadata.TryGetValue("StellarModuleId", out string? dependencyId)
                && !string.Equals(dependencyId, id, StringComparison.Ordinal))
                modules[dependencyId] = metadata;
        }

        // The compiler sees transitive references too. A module another referenced module depends on is not a
        // dependency of this project: only the direct ones are checked
        HashSet<string> transitive = new(StringComparer.Ordinal);
        foreach (Dictionary<string, string> metadata in modules.Values)
            AddDependencies(metadata, modules, transitive);

        foreach (KeyValuePair<string, Dictionary<string, string>> pair in modules
                     .Where(pair => !transitive.Contains(pair.Key))
                     .OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            string dependencyId = pair.Key;
            pair.Value.TryGetValue("StellarModuleLevel", out string? dependencyLevelName);
            int? dependencyLevel = ModuleLevels.Parse(dependencyLevelName);

            // ── STLR0201: module level violation ──────────────────────────
            if (dependencyLevel is { } known && !ModuleLevels.MayDependOn(level.Value, known))
                context.ReportDiagnostic(Diagnostic.Create(
                    StellarDiagnosticDescriptors.ModuleLevelViolation,
                    Location.None,
                    id, ModuleLevels.Name(level.Value), dependencyId, ModuleLevels.Name(known)));

            // ── STLR0202: undeclared module dependency ────────────────────
            if (isModule && !ModuleLevels.IsCore(dependencyId) && !declared.Contains(dependencyId))
                context.ReportDiagnostic(Diagnostic.Create(
                    StellarDiagnosticDescriptors.UndeclaredModuleDependency,
                    Location.None,
                    id, dependencyId));
        }

        if (isModule)
            CheckDescriptor(context, id);
    }
}

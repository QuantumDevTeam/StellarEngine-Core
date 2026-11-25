using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Stellar.Sdk.Analyzers.Configuration;
using Stellar.Sdk.Analyzers.Diagnostics;
using Stellar.Sdk.Analyzers.Internal;

namespace Stellar.Sdk.Analyzers.Analyzers.EntryPoint;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StellarEntryPointAnalyzer : DiagnosticAnalyzer
{
    // ── Well-known Stellar type names ─────────────────────────────────────

    private const string StellarProjectFileExtension = ".stellar.project";
    private const string DefaultEntryPointKeyword = "default";
    private const string DefaultEntryPointTypeSuffix = "EntryPoint";

    private const string EntryPointBaseFqn = "Stellar.Kernel.EntryPoint.StellarEntryPoint";
    private const string EntryPointAttributeFqn = "Stellar.Kernel.EntryPoint.StellarEntryPointAttribute";

    // ── Helpers ───────────────────────────────────────────────────────────

    private static string ResolveEntryPointTypeName(
        string rawValue,
        string? assemblyName
    )
    {
        if (string.IsNullOrEmpty(rawValue)
            || string.Equals(rawValue, DefaultEntryPointKeyword, StringComparison.OrdinalIgnoreCase))
            // Convention: {AssemblyName}.EntryPoint in the root namespace
            return string.IsNullOrEmpty(assemblyName)
                ? DefaultEntryPointTypeSuffix
                : $"{assemblyName}.{DefaultEntryPointTypeSuffix}";

        return rawValue; // fully-qualified name written by the developer
    }

    // ── DiagnosticAnalyzer ────────────────────────────────────────────────

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            StellarDiagnosticDescriptors.EntryPointTypeNotFound,
            StellarDiagnosticDescriptors.EntryPointMissingInheritance,
            StellarDiagnosticDescriptors.EntryPointMissingAttribute,
            StellarDiagnosticDescriptors.NoEntryPointInModule
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    // ── Compilation start ─────────────────────────────────────────────────

    private static void OnCompilationStart(CompilationStartAnalysisContext ctx)
    {
        // 1. Locate .stellar.project in AdditionalFiles
        AdditionalText? configFile = ctx.Options.AdditionalFiles
            .FirstOrDefault(f => f.Path.EndsWith(
                StellarProjectFileExtension,
                StringComparison.OrdinalIgnoreCase));

        if (configFile is null) return;

        SourceText? sourceText = configFile.GetText(ctx.CancellationToken);
        if (sourceText is null) return;

        // 2. Parse config — bail silently on malformed JSON
        //    (the build task will produce a proper build error for that)
        ConfigReadResult readResult = StellarProjectConfigReader.TryRead(sourceText.ToString());
        if (!readResult.IsSuccess) return;

        string rawEntryPointValue =
            readResult.Config!.Project?.StellarEntryPoint?.Trim()
            ?? DefaultEntryPointKeyword;

        // 3. Resolve well-known Stellar symbols — they may be null if the
        //    kernel assembly isn't referenced, which is fine for pure
        //    library projects; checks will be skipped gracefully.
        INamedTypeSymbol? baseTypeSymbol =
            ctx.Compilation.GetTypeByMetadataName(EntryPointBaseFqn);
        INamedTypeSymbol? attributeTypeSymbol =
            ctx.Compilation.GetTypeByMetadataName(EntryPointAttributeFqn);

        // 4. Collect all types in this compilation that carry [StellarEntryPoint].
        //    ConcurrentBag because RegisterSymbolAction may run in parallel.
        ConcurrentBag<INamedTypeSymbol> decoratedTypes = new();

        if (attributeTypeSymbol is not null)
            ctx.RegisterSymbolAction(symbolCtx =>
            {
                var namedType = (INamedTypeSymbol)symbolCtx.Symbol;
                if (namedType.HasAttribute(attributeTypeSymbol))
                    decoratedTypes.Add(namedType);
            }, SymbolKind.NamedType);

        // 5. All validation happens at the very end — after all symbols are known.
        ctx.RegisterCompilationEndAction(endCtx =>
            AnalyzeCompilationEnd(
                endCtx,
                configFile.Path,
                sourceText,
                rawEntryPointValue,
                baseTypeSymbol,
                attributeTypeSymbol,
                decoratedTypes));
    }

    // ── Compilation end ───────────────────────────────────────────────────

    private static void AnalyzeCompilationEnd(
        CompilationAnalysisContext ctx,
        string configFilePath,
        Microsoft.CodeAnalysis.Text.SourceText sourceText,
        string rawEntryPointValue,
        INamedTypeSymbol? baseTypeSymbol,
        INamedTypeSymbol? attributeTypeSymbol,
        ConcurrentBag<INamedTypeSymbol> decoratedTypes
    )
    {
        Compilation compilation = ctx.Compilation;

        // Location of the value token inside the .stellar.project file
        Location configValueLocation = JsonTextLocator.FindValueLocation(
            sourceText, configFilePath, "StellarEntryPoint");

        // ── STLR0104: no entry-point types at all in the module ───────────
        //    Report this independently of the config; it fires even when
        //    the specified type doesn't exist yet.
        if (attributeTypeSymbol is not null && decoratedTypes.IsEmpty)
            ctx.ReportDiagnostic(Diagnostic.Create(
                StellarDiagnosticDescriptors.NoEntryPointInModule,
                configValueLocation,
                compilation.AssemblyName ?? "<module>"));

        // ── Resolve the configured type ───────────────────────────────────
        string resolvedTypeName = ResolveEntryPointTypeName(rawEntryPointValue, compilation.AssemblyName);
        INamedTypeSymbol? configuredType = compilation.GetTypeByMetadataName(resolvedTypeName);

        // ── STLR0101: type not found ──────────────────────────────────────
        if (configuredType is null)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                StellarDiagnosticDescriptors.EntryPointTypeNotFound,
                configValueLocation,
                resolvedTypeName));
            return; // remaining checks are pointless without the symbol
        }

        // Use the first declaration location on the type itself for STLR0102/0103
        // so the squiggle lands on the class keyword.
        Location typeLocation = configuredType.Locations.FirstOrDefault() ?? Location.None;

        // ── STLR0102: missing inheritance ─────────────────────────────────
        if (baseTypeSymbol is not null
            && !configuredType.InheritsFrom(baseTypeSymbol))
            ctx.ReportDiagnostic(Diagnostic.Create(
                StellarDiagnosticDescriptors.EntryPointMissingInheritance,
                typeLocation,
                resolvedTypeName));

        // ── STLR0103: missing attribute ───────────────────────────────────
        if (attributeTypeSymbol is not null
            && !configuredType.HasAttribute(attributeTypeSymbol))
            ctx.ReportDiagnostic(Diagnostic.Create(
                StellarDiagnosticDescriptors.EntryPointMissingAttribute,
                typeLocation,
                resolvedTypeName));
    }
}

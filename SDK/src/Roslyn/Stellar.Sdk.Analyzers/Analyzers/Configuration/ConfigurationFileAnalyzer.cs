using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Stellar.Sdk.Analyzers.Diagnostics;
using Stellar.Sdk.Analyzers.Internal;

namespace Stellar.Sdk.Analyzers.Analyzers.Configuration;

/// <summary>
/// Points at the mistake in a malformed <c>.stellar.project</c> or <c>.stellar.module</c>, in the file itself:
/// the IDE shows it where it is, not as a build task failure.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConfigurationFileAnalyzer
    : DiagnosticAnalyzer
{
    private static readonly string[] Extensions = [".stellar.project", ".stellar.module"];

    // ── DiagnosticAnalyzer ────────────────────────────────────────────────

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(StellarDiagnosticDescriptors.MalformedConfigurationFile);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterAdditionalFileAction(Analyze);
    }

    // ── Additional file action ────────────────────────────────────────────

    private static void Analyze(AdditionalFileAnalysisContext context)
    {
        string path = context.AdditionalFile.Path;
        if (Array.TrueForAll(Extensions, extension => !path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)))
            return;

        SourceText? text = context.AdditionalFile.GetText(context.CancellationToken);
        if (text is null || JsonSyntaxChecker.TryCheck(text.ToString(), out int position, out string message))
            return;

        var span = new TextSpan(Math.Min(position, Math.Max(0, text.Length - 1)), text.Length == 0 ? 0 : 1);
        var location = Location.Create(path, span, text.Lines.GetLinePositionSpan(span));

        // ── STLR0301: malformed configuration file ────────────────────────
        context.ReportDiagnostic(Diagnostic.Create(
            StellarDiagnosticDescriptors.MalformedConfigurationFile,
            location,
            System.IO.Path.GetFileName(path), message));
    }
}

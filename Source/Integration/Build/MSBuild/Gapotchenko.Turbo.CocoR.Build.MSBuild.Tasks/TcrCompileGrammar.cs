using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks;

/// <summary>
/// Turbo Coco/R grammar compilation task for MSBuild.
/// </summary>
public sealed class TcrCompileGrammar : ToolTask
{
    public TcrCompileGrammar()
    {
        UseCommandProcessor = true;
    }

    protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

    //protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_Grammar;

    [Required]
    public string Grammar
    {
        get => m_Grammar ?? throw new InvalidOperationException("Grammar is not set.");
        set => m_Grammar = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected override string ToolName => "turbo-coco";

    protected override string GenerateFullPathToTool() =>
        Path.GetFullPath(
            Path.Combine(
                typeof(TcrCompileGrammar).Assembly.Location,
                @"..\..\..\..",
                ToolName));

    protected override string GenerateCommandLineCommands() => $"\"{GenerateFullPathToTool()}\" --int-call project compile-grammar \"{Grammar}\" -f";
}

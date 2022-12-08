using Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks.Properties;
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
        EchoOff = true;
    }

    protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

    protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_Grammar;

    /// <summary>
    /// Gets or sets the grammar file path.
    /// </summary>
    [Required]
    public string Grammar
    {
        get => m_Grammar ?? throw new InvalidOperationException(string.Format(Resources.XNotSet, nameof(Grammar)));
        set => m_Grammar = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the programming language.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the programming language version.
    /// </summary>
    public string? LanguageVersion { get; set; }

    /// <summary>
    /// Gets or sets the root namespace of a project.
    /// </summary>
    public string? RootNamespace { get; set; }

    /// <summary>
    /// Gets or sets the project directory path.
    /// </summary>
    public string? ProjectDir { get; set; }

    protected override string ToolName => "turbo-coco";

    protected override string GenerateFullPathToTool() =>
        Path.GetFullPath(
            Path.Combine(
                typeof(TcrCompileGrammar).Assembly.Location,
                @"..\..\..\..",
                ToolName));

    protected override string GenerateCommandLineCommands()
    {
        var clb = new CommandLineBuilder();
        clb.AppendFileNameIfNotNull(GenerateFullPathToTool());
        clb.AppendSwitch("--no-logo");
        clb.AppendSwitch("--int-call");
        clb.AppendSwitch("compile-project-grammar");
        clb.AppendFileNameIfNotNull(Grammar);
        clb.AppendSwitch("-q");
        clb.AppendSwitch("-f");

        return clb.ToString();
    }
}

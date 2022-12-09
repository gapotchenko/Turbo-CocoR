using Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks.Properties;
using Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks.Utils;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks;

/// <summary>
/// Turbo Coco/R grammar compilation task for MSBuild.
/// </summary>
public sealed class TcrCompileGrammar : ToolTask
{
    public TcrCompileGrammar()
    {
        // Use command interpreter to execute the command line.
        UseCommandProcessor = true;

        // Turn off echo for the command interpreter.
        //EchoOff = true;
    }

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

    /// <summary>
    /// Creates the command line to execute.
    /// </summary>
    /// <returns>The command line.</returns>
    protected override string GenerateCommandLineCommands()
    {
        var clb = new CommandLineBuilderEx();

        clb.AppendFileNameIfNotNull(GenerateFullPathToTool());
        clb.AppendSwitch("--no-logo");
        clb.AppendSwitch("--int-call");
        clb.AppendSwitch("compile-project-grammar");
        clb.AppendFileNameIfNotNull(Grammar);
        clb.AppendSwitch("-q");
        clb.AppendSwitch("-f");

        var language = Language;
        if (!string.IsNullOrEmpty(language))
        {
            clb.AppendParameter("--lang", language);

            var languageVersion = LanguageVersion;
            if (!string.IsNullOrEmpty(languageVersion))
                clb.AppendParameter("--lang-version", languageVersion);
        }

        if (TryGetNamespaceHint() is not null and var namespaceHint)
            clb.AppendProperty("NamespaceHint", namespaceHint);

        return clb.ToString();
    }

    protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

    protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

    string? TryGetNamespaceHint()
    {
        string? namespaceHint = null;

        var rootNamespace = RootNamespace;
        var projectDir = ProjectDir;

        if (!string.IsNullOrEmpty(rootNamespace) && !string.IsNullOrEmpty(projectDir))
        {
            string relativePath = PathUtil.GetRelativePath(projectDir, Path.GetDirectoryName(Grammar) ?? ".");
            if (!Path.IsPathRooted(relativePath))
            {
                string? relativeNamespace;
                if (relativePath == ".")
                {
                    relativeNamespace = "";
                }
                else
                {
                    var p = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (p.Length != 0 && p[0] == "..")
                        relativeNamespace = null;
                    else
                        relativeNamespace = string.Join(LanguageNamespaceSeparator, p.Select(EscapeLanguageIdentifier));
                }

                if (relativeNamespace != null)
                    namespaceHint = CombineLanguageNamespaces(rootNamespace, relativeNamespace);
            }
        }

        return namespaceHint;
    }

    [return: NotNullIfNotNull(nameof(id))]
    string EscapeLanguageIdentifier(string id)
    {
        if (string.IsNullOrEmpty(id))
            return id;

        id = id.Replace(' ', '_');

        if (char.IsDigit(id[0]))
            id = "_" + id;

        return id;
    }

    string LanguageNamespaceSeparator => ".";

    string CombineLanguageNamespaces(string a, string b)
    {
        if (a.Length == 0)
            return b;
        if (b.Length == 0)
            return a;
        else
            return a + LanguageNamespaceSeparator + b;
    }
}

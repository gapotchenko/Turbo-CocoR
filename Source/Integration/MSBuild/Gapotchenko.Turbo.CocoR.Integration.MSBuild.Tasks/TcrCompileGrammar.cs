using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Diagnostics;
using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;
using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Properties;
using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Utils;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks;

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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_Language;

    /// <summary>
    /// Gets or sets the programming language.
    /// </summary>
    public string? Language
    {
        get => m_Language;
        set
        {
            if (value != m_Language)
            {
                m_Language = value;
                m_CachedLanguageProvider = null;
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_LanguageVersion;

    /// <summary>
    /// Gets or sets the programming language version.
    /// </summary>
    public string? LanguageVersion
    {
        get => m_LanguageVersion;
        set
        {
            if (value != m_LanguageVersion)
            {
                m_LanguageVersion = value;
                m_CachedLanguageProvider = null;
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    ILanguageProvider? m_CachedLanguageProvider;

    ILanguageProvider? LanguageProvider => m_CachedLanguageProvider ??= GetLanguageProviderCore();

    ILanguageProvider? GetLanguageProviderCore()
    {
        var language = Language;
        if (string.IsNullOrEmpty(language))
            return null;
        return LanguageService.Default.CreateProvider(language, LanguageVersion);
    }

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

        if (LanguageProvider is not null and var languageProvider)
        {
            clb.AppendParameter("--lang", languageProvider.Language);
            clb.AppendParameter("--lang-version", languageProvider.LanguageVersion);
        }

        if (TryGetNamespaceHint() is not null and var namespaceHint)
            clb.AppendProperty("NamespaceHint", namespaceHint);

        return clb.ToString();
    }

    protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

    protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

    protected override bool ValidateParameters()
    {
        try
        {
            _ = Grammar;
            _ = LanguageProvider;
            return true;
        }
        catch (Exception e)
        {
            LogException(e);
            return false;
        }
    }

    public override bool Execute()
    {
        try
        {
            return base.Execute();
        }
        catch (Exception e)
        {
            LogException(e);
            return false;
        }
    }

    void LogException(Exception e)
    {
        if (e.TryGetCode() is not null and var code)
            Log.LogError(null, code, null, null, 0, 0, 0, 0, "{0}", e.Message);
        else
            Log.LogErrorFromException(e);
    }

    string? TryGetNamespaceHint()
    {
        var rootNamespace = RootNamespace;
        var projectDir = ProjectDir;

        if (!string.IsNullOrEmpty(rootNamespace) &&
            !string.IsNullOrEmpty(projectDir) &&
            LanguageProvider is not null and var languageProvider)
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
                        relativeNamespace = string.Join(languageProvider.NamespaceSeparator, p.Select(languageProvider.EscapeIdentifier));
                }

                if (relativeNamespace != null)
                    return languageProvider.CombineNamespaces(rootNamespace, relativeNamespace);
            }
        }

        return null;
    }
}

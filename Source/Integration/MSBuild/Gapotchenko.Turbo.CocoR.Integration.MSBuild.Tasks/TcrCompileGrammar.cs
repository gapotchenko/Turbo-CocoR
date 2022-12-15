using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;
using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Properties;
using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Utilities;
using Microsoft.Build.Framework;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks;

/// <summary>
/// Turbo Coco/R grammar compilation task for MSBuild.
/// </summary>
public sealed class TcrCompileGrammar : TcrToolTask
{
    public TcrCompileGrammar()
    {
#if DEBUG
        //EchoOff = false;
#endif
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
    /// Gets or sets the scanner frame file path.
    /// </summary>
    public string? ScannerFrame { get; set; }

    /// <summary>
    /// Gets or sets the parser frame file path.
    /// </summary>
    public string? ParserFrame { get; set; }

    /// <summary>
    /// Gets or sets the copyright frame file path.
    /// </summary>
    public string? CopyrightFrame { get; set; }

    /// <summary>
    /// Gets or sets the preface frame file path.
    /// </summary>
    public string? PrefaceFrame { get; set; }

    /// <summary>
    /// Gets or sets the scanner file path.
    /// </summary>
    public string? Scanner { get; set; }

    /// <summary>
    /// Gets or sets the parser file path.
    /// </summary>
    public string? Parser { get; set; }

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

    /// <summary>
    /// Creates the command line to execute.
    /// </summary>
    /// <returns>The command line.</returns>
    protected override string GenerateCommandLineCommands()
    {
        var clb = new CommandLineBuilderEx();

        clb.AppendFileNameIfNotNull(GenerateFullPathToTool());
        clb.AppendSwitch("--no-logo");
        clb.AppendSwitch("-q");
        clb.AppendSwitch("-f");

        if (!string.IsNullOrEmpty(ScannerFrame))
            clb.AppendProperty("ScannerFrame", ScannerFrame);

        if (!string.IsNullOrEmpty(ParserFrame))
            clb.AppendProperty("ParserFrame", ParserFrame);

        if (!string.IsNullOrEmpty(CopyrightFrame))
            clb.AppendProperty("CopyrightFrame", CopyrightFrame);

        if (!string.IsNullOrEmpty(PrefaceFrame))
            clb.AppendProperty("PrefaceFrame", PrefaceFrame);

        if (!string.IsNullOrEmpty(Scanner))
            clb.AppendProperty("Scanner", Scanner);

        if (!string.IsNullOrEmpty(Parser))
            clb.AppendProperty("Parser", Parser);

        if (LanguageProvider is not null and var languageProvider)
        {
            clb.AppendParameter("--lang", languageProvider.Language);
            clb.AppendParameter("--lang-version", languageProvider.LanguageVersion);
        }

        if (TryGetNamespaceHint() is not null and var namespaceHint)
        {
            // Namespace hint is essential for the newly scaffolded .atg files.
            clb.AppendProperty("NamespaceHint", namespaceHint);
        }

        clb.AppendSwitch("--int-call");
        clb.AppendSwitch("compile-project-grammar");
        clb.AppendFileNameIfNotNull(Grammar);

        return clb.ToString();
    }

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

    /// <summary>
    /// Infers the namespace of the grammar file.
    /// </summary>
    /// <returns>The inferred namespace or <see langword="null"/> if a namespace cannot be inferred.</returns>
    string? TryGetNamespaceHint()
    {
        var rootNamespace = RootNamespace;
        var projectDir = ProjectDir;

        if (!string.IsNullOrEmpty(rootNamespace) &&
            !string.IsNullOrEmpty(projectDir) &&
            LanguageProvider is not null and var languageProvider)
        {
            string relativePath = PathUtil.GetRelativePath(projectDir, Empty.NullifyWhiteSpace(Path.GetDirectoryName(Grammar)) ?? ".");
            if (!Path.IsPathRooted(relativePath))
            {
                string? relativeNamespace;
                if (relativePath == ".")
                {
                    relativeNamespace = string.Empty;
                }
                else
                {
                    var p = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (p.Length != 0 && p[0] == "..")
                        relativeNamespace = null;
                    else
                        relativeNamespace = languageProvider.CombineNamespaces(p.Select(x => languageProvider.EscapeIdentifier(x)));
                }

                if (relativeNamespace != null)
                    return languageProvider.CombineNamespaces(rootNamespace, relativeNamespace);
            }
        }

        return null;
    }
}

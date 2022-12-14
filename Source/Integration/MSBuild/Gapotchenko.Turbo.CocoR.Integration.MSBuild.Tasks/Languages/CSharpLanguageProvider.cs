namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;

sealed class CSharpLanguageProvider : ILanguageProvider
{
    public CSharpLanguageProvider(string? languageVersion)
    {
        if (languageVersion == null)
            m_LanguageVersion = new Version(8, 0);
        else
            m_LanguageVersion = Version.Parse(languageVersion);
    }

    readonly Version m_LanguageVersion;

    public string Language => "C#";

    public string LanguageVersion => m_LanguageVersion.ToString();

    public string NamespaceSeparator => ".";

    [return: NotNullIfNotNull(nameof(id))]
    public string? EscapeIdentifier(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return id;

        id = id.Replace(' ', '_');

        if (char.IsDigit(id[0]))
            id = "_" + id;

        return id;
    }

    public string CombineNamespaces(string a, string b)
    {
        if (a.Length == 0)
            return b;
        if (b.Length == 0)
            return a;
        else
            return a + NamespaceSeparator + b;
    }

    public string CombineNamespaces(IEnumerable<string> ns) =>
        string.Join(NamespaceSeparator, ns.Where(x => x.Length != 0));
}

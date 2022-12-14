namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;

interface ILanguageProvider
{
    string Language { get; }

    string LanguageVersion { get; }

    string NamespaceSeparator { get; }

    [return: NotNullIfNotNull(nameof(id))]
    string? EscapeIdentifier(string? id);

    string CombineNamespaces(string a, string b);

    string CombineNamespaces(IEnumerable<string> ns);
}

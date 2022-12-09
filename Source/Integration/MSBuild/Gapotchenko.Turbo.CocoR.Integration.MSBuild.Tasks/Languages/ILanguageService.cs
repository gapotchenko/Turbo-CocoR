namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;

interface ILanguageService
{
    ILanguageProvider CreateProvider(string language, string? languageVersion);
}

using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Diagnostics;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Languages;

sealed class LanguageService : ILanguageService
{
    LanguageService()
    {
    }

    public static ILanguageService Default { get; } = new LanguageService();

    public ILanguageProvider CreateProvider(string language, string? languageVersion) =>
        language switch
        {
            "C#" => new CSharpLanguageProvider(languageVersion),
            _ => throw
                new Exception(string.Format("Turbo Coco/R does not support {0} programming language.", language))
                .Categorize("TCR0001"),
        };
}

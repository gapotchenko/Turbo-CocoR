#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

public interface IScaffoldingService
{
    TextReader? TryOpenTemplate(string templateName);

    TextReader OpenTemplate(string templateName);

    void SaveTemplate(TextReader template, string destinationFilePath);
}

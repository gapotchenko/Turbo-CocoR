#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

public interface IScaffoldingService
{
    TextReader OpenTemplate(string templateName);

    TextReader? TryOpenTemplate(string templateName);

    string CreateItem(string category, string name);
}

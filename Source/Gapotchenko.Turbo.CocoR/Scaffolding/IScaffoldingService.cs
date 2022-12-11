#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

interface IScaffoldingService
{
    TextReader OpenTemplate(string templateName);

    TextReader? TryOpenTemplate(string templateName);

    ScaffoldingItemCategory GetItemCategory(string s);

    string CreateItem(ScaffoldingItemCategory category, string name);
}

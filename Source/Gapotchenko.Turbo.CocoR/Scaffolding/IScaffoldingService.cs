#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

public interface IScaffoldingService
{
    TextReader? TryOpenTemplate(string templateName);
}

using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

[Export(typeof(IScaffoldingService))]
sealed class ScaffoldingService : IScaffoldingService
{
    public TextReader? TryOpenTemplate(string templateName)
    {
        var type = typeof(ScaffoldingService);
        var stream = type.Assembly.GetManifestResourceStream(type, "Templates." + templateName);
        if (stream == null)
            return null;

        return new StreamReader(stream);
    }
}

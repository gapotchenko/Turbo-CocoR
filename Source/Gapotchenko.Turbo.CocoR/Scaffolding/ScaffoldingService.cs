using Gapotchenko.Turbo.CocoR.IO;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

[Export(typeof(IScaffoldingService))]
sealed class ScaffoldingService : IScaffoldingService
{
    [ImportingConstructor]
    public ScaffoldingService(IIOService ioService)
    {
        m_IOService = ioService;
    }

    readonly IIOService m_IOService;

    public TextReader? TryOpenTemplate(string templateName)
    {
        var type = typeof(ScaffoldingService);
        var stream = type.Assembly.GetManifestResourceStream(type, "Templates." + templateName);
        if (stream == null)
            return null;

        return new StreamReader(stream);
    }

    public TextReader OpenTemplate(string templateName) =>
        TryOpenTemplate(templateName) ??
        throw new Exception($"Scaffolding template \"{templateName}\" does not exist.");

    public void SaveTemplate(TextReader template, string destinationFilePath)
    {
        m_IOService.CreateFileBackup(destinationFilePath);

        using var file = File.CreateText(destinationFilePath);
        for (; ; )
        {
            var line = template.ReadLine();
            if (line == null)
                break;
            file.WriteLine(line);
        }
    }
}

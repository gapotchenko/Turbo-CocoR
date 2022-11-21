using Gapotchenko.Turbo.CocoR.IO;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

[Export(typeof(IScaffoldingService))]
sealed class ScaffoldingService : IScaffoldingService
{
    [ImportingConstructor]
    public ScaffoldingService(IOptionsService optionsService, IIOService ioService)
    {
        m_OptionsService = optionsService;
        m_IOService = ioService;
    }

    readonly IOptionsService m_OptionsService;
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

    void SaveTemplate(TextReader template, string destinationFileName)
    {
        string destinationFilePath = Path.Combine(m_OptionsService.OutputDirectoryPath, destinationFileName);

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

    public void CreateItem(string category, string name)
    {
        if (category != "frame")
            throw new Exception($"Unknown scaffolding category \"{category}\" specified.");

        var templateName =
            name switch
            {
                "scanner" => "Scanner.frame",
                "parser" => "Parser.frame",
                "preface" => "Preface.frame",
                _ => null
            };

        if (templateName == null)
            throw new Exception($"Unknown scaffolding item name \"{name}\" specified.");

        using var template = OpenTemplate(templateName);
        SaveTemplate(template, templateName);

        Console.WriteLine($"New \"{templateName}\" file created successfully.");
    }
}

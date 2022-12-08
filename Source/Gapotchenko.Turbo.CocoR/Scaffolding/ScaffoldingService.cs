using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.IO;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;
using System.Globalization;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

[Export(typeof(IScaffoldingService))]
sealed class ScaffoldingService : IScaffoldingService
{
    [ImportingConstructor]
    public ScaffoldingService(
        IOptionsService optionsService,
        IIOService ioService,
        IProductInformationService productInformationService)
    {
        m_OptionsService = optionsService;
        m_IOService = ioService;
        m_ProductInformationService = productInformationService;
    }

    readonly IOptionsService m_OptionsService;
    readonly IIOService m_IOService;
    readonly IProductInformationService m_ProductInformationService;

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

    void ExtractTemplate(TextReader template, string destinationFileName, IDictionary<string, object?> variables)
    {
        string destinationFilePath = Path.Combine(m_OptionsService.OutputDirectoryPath, destinationFileName);

        var text = template.ReadToEnd();

        var st = new Antlr4.StringTemplate.Template(text, '%', '%');
        foreach (var i in variables)
            st.Add(i.Key, i.Value);
        text = st.Render(CultureInfo.InvariantCulture);

        var reader = new StringReader(text);

        m_IOService.CreateFileBackup(destinationFilePath);
        using var file = File.CreateText(destinationFilePath);

        // Write the text in a line by line fashion to convert the new line characters to OS-native format.
        for (; ; )
        {
            var line = reader.ReadLine();
            if (line == null)
                break;
            file.WriteLine(line);
        }
    }

    public string CreateItem(string category, string name)
    {
        string? templateName;
        string outputFilePath;

        if (category == "frame")
        {
            templateName =
                name switch
                {
                    "scanner" => "Scanner.frame",
                    "parser" => "Parser.frame",
                    "preface" => "Preface.frame",
                    _ => null
                };

            if (templateName == null)
                throw new Exception($"Unknown scaffolding item name \"{name}\" specified.");

            outputFilePath = templateName;
        }
        else if (category == "grammar")
        {
            templateName = "Grammar.atg";
            outputFilePath = name;
        }
        else
        {
            throw new Exception($"Unknown scaffolding category \"{category}\" specified.");
        }

        using var template = OpenTemplate(templateName);

        string outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilePath);
        var properties = m_OptionsService.Properties;
        var mode = properties.GetValueOrDefault("Mode", "Standalone");
        string? @namespace = m_OptionsService.Namespace ?? properties.GetValueOrDefault("NamespaceHint");

        var variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["command"] = m_ProductInformationService.Command,
            ["compatibility"] = $"{m_ProductInformationService.Name} {m_ProductInformationService.FormalVersion}",
            ["output_file_name"] = Path.GetFileName(outputFilePath),
            ["coco_lang"] = outputFileNameWithoutExtension.Replace(' ', '_').Replace('.', '_'),
            ["lang"] = "C#",
            ["lang_version"] = "7.0",
            ["lang_namespace"] = @namespace,
            ["has_lang_namespace"] = @namespace != null,
            ["standalone"] = mode.Equals("Standalone", StringComparison.OrdinalIgnoreCase) 
        };

        ExtractTemplate(template, outputFilePath, variables);

        return outputFilePath;
    }
}

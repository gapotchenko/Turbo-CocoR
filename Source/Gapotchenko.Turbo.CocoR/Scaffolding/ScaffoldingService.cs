using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.IO;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;
using System.Globalization;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Scaffolding;

[Shared]
[Export(typeof(IScaffoldingService))]
sealed class ScaffoldingService : IScaffoldingService
{
    [ImportingConstructor]
    public ScaffoldingService(
        IOptionsService optionsService,
        IIOService ioService,
        IProductInformationService productInformationService,
        Lazy<ICodeGenerationService> codeGenerationService)
    {
        m_OptionsService = optionsService;
        m_IOService = ioService;
        m_ProductInformationService = productInformationService;
        m_CodeGenerationService = codeGenerationService;
    }

    readonly IOptionsService m_OptionsService;
    readonly IIOService m_IOService;
    readonly IProductInformationService m_ProductInformationService;
    readonly Lazy<ICodeGenerationService> m_CodeGenerationService;

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

    public ScaffoldingItemCategory GetItemCategory(string s) =>
        s switch
        {
            "frame" => ScaffoldingItemCategory.Frame,
            "grammar" => ScaffoldingItemCategory.Grammar,
            null => throw new ArgumentNullException(nameof(s)),
            _ => throw new Exception($"Unknown scaffolding category \"{s}\".")
        };

    public string CreateItem(ScaffoldingItemCategory category, string name)
    {
        string? templateName;
        string outputFilePath;

        switch (category)
        {
            case ScaffoldingItemCategory.Frame:
                templateName =
                    name switch
                    {
                        ScaffoldingItemNames.Frame.Scanner => FrameFileNames.Scanner,
                        ScaffoldingItemNames.Frame.Parser => FrameFileNames.Parser,
                        ScaffoldingItemNames.Frame.Preface => FrameFileNames.Preface,
                        _ => null
                    };

                if (templateName == null)
                    throw new Exception($"Unknown scaffolding item name \"{name}\" specified.");

                outputFilePath = m_CodeGenerationService.Value.TryGetExplicitFrameFilePath(templateName) ?? templateName;
                break;

            case ScaffoldingItemCategory.Grammar:
                templateName = "Grammar.atg";
                outputFilePath = name;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(category));
        }

        using var template = OpenTemplate(templateName);

        string outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilePath);
        var properties = m_OptionsService.Properties;
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
            ["standalone"] = !m_OptionsService.IntCall
        };

        ExtractTemplate(template, outputFilePath, variables);

        return outputFilePath;
    }
}

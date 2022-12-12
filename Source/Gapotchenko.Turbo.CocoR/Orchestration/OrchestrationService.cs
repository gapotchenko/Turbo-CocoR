using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Scaffolding;
using System.Composition;

namespace Gapotchenko.Turbo.CocoR.Orchestration;

[Shared]
[Export(typeof(IOrchestrationService))]
sealed class OrchestrationService : IOrchestrationService
{
    [ImportingConstructor]
    public OrchestrationService(
        IOptionsService optionsService,
        Lazy<Compiler> compiler,
        Lazy<IScaffoldingService> scaffoldingService,
        Lazy<ICodeGenerationService> codeGenerationService)
    {
        m_OptionsService = optionsService;
        m_Compiler = compiler;
        m_ScaffoldingService = scaffoldingService;
        m_CodeGenerationService = codeGenerationService;
    }

    readonly IOptionsService m_OptionsService;
    readonly Lazy<Compiler> m_Compiler;
    readonly Lazy<IScaffoldingService> m_ScaffoldingService;
    readonly Lazy<ICodeGenerationService> m_CodeGenerationService;

    public void CompileGrammar()
    {
        string filePath = m_OptionsService.SourceFilePath;
        m_OptionsService.SourceDirectoryPath = Path.GetDirectoryName(filePath);
        m_Compiler.Value.Compile(filePath);
    }

    public void Scaffold(string itemCategory, IEnumerable<string> itemNames)
    {
        bool quiet = m_OptionsService.Quiet;
        var scaffoldingService = m_ScaffoldingService.Value;

        var category = scaffoldingService.GetItemCategory(itemCategory);
        foreach (var itemName in itemNames)
        {
            var templateName = scaffoldingService.CreateItem(category, itemName);
            if (!quiet)
                Console.WriteLine($"New \"{templateName}\" file created successfully.");
        }
    }

    public void CompileProjectGrammar(string grammarFilePath)
    {
        var grammarFile = new FileInfo(grammarFilePath);

        if (!grammarFile.Exists)
            throw new Exception(string.Format("Grammar file \"{0}\" does not exist.", grammarFilePath)).Categorize("TCR0003");

        bool grammarIsEmpty = grammarFile.Length == 0;
        if (!grammarIsEmpty)
        {
            // Use a text reader because the file can have a BOM but still be empty.
            using var tr = grammarFile.OpenText();
            grammarIsEmpty = tr.Read() == -1;
        }

        if (grammarIsEmpty)
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Grammar, grammarFilePath);

        m_OptionsService.SourceDirectoryPath = Path.GetDirectoryName(grammarFilePath);

        var tsSync =
            m_OptionsService.Properties.GetValueOrDefault("SyncTimestamp") == "true" ?
                new TimestampSynchronizer() :
                null;

        tsSync?.AddFile(grammarFilePath);

        var cgs = m_CodeGenerationService.Value;

        var scannerFramePath = cgs.GetFrameFilePath(FrameFileNames.Scanner);
        if (!File.Exists(scannerFramePath))
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Frame, ScaffoldingItemNames.Frame.Scanner);
        tsSync?.AddFile(scannerFramePath);

        var parserFramePath = cgs.GetFrameFilePath(FrameFileNames.Parser);
        if (!File.Exists(parserFramePath))
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Frame, ScaffoldingItemNames.Frame.Parser);
        tsSync?.AddFile(parserFramePath);

        if (tsSync != null)
        {
            tsSync.AddFile(cgs.GetFrameFilePath(FrameFileNames.Copyright));
            tsSync.AddFile(cgs.GetFrameFilePath(FrameFileNames.Preface));
        }

        m_Compiler.Value.Compile(grammarFilePath);

        if (tsSync?.Timestamp is not null and var timestamp)
        {
            File.SetLastWriteTimeUtc(cgs.GetCodeFilePath("Scanner.cs"), timestamp.Value);
            File.SetLastWriteTimeUtc(cgs.GetCodeFilePath("Parser.cs"), timestamp.Value);
        }
    }
}

using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;
using Gapotchenko.Turbo.CocoR.IO;
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
        Lazy<ICodeGenerationService> codeGenerationService,
        Lazy<IIOService> ioService)
    {
        m_OptionsService = optionsService;
        m_Compiler = compiler;
        m_ScaffoldingService = scaffoldingService;
        m_CodeGenerationService = codeGenerationService;
        m_IOService = ioService;
    }

    readonly IOptionsService m_OptionsService;
    readonly Lazy<Compiler> m_Compiler;
    readonly Lazy<IScaffoldingService> m_ScaffoldingService;
    readonly Lazy<ICodeGenerationService> m_CodeGenerationService;
    readonly Lazy<IIOService> m_IOService;

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

    /// <summary>
    /// Compiles the grammar for a project.
    /// Used by MSBuild integration.
    /// </summary>
    /// <param name="grammarFilePath">The grammar file path.</param>
    public void CompileProjectGrammar(string grammarFilePath)
    {
        var grammarFile = new FileInfo(grammarFilePath);

        if (!grammarFile.Exists)
            throw new Exception(string.Format("Grammar file \"{0}\" does not exist.", grammarFilePath)).Categorize("TCR0003");

        var tsSync =
            string.Equals(m_OptionsService.Properties.GetValueOrDefault("SyncTimestamp"), "true", StringComparison.OrdinalIgnoreCase) ?
                new TimestampSynchronizer() :
                null;

        bool grammarIsEmpty = grammarFile.Length == 0;
        if (!grammarIsEmpty)
        {
            // Use a text reader because the file can have a BOM but still be empty.
            using var tr = grammarFile.OpenText();
            grammarIsEmpty = tr.Read() == -1;
        }

        if (grammarIsEmpty)
        {
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Grammar, grammarFilePath);
            tsSync = null;
        }
        else
        {
            tsSync?.AddFile(grammarFilePath);
        }

        m_OptionsService.SourceDirectoryPath = Path.GetDirectoryName(grammarFilePath);

        var cgs = m_CodeGenerationService.Value;

        var scannerFramePath = cgs.GetFrameFilePath(FrameFileNames.Scanner);
        if (!File.Exists(scannerFramePath))
        {
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Frame, ScaffoldingItemNames.Frame.Scanner);
            tsSync = null;
        }
        else
        {
            tsSync?.AddFile(scannerFramePath);
        }

        var parserFramePath = cgs.GetFrameFilePath(FrameFileNames.Parser);
        if (!File.Exists(parserFramePath))
        {
            m_ScaffoldingService.Value.CreateItem(ScaffoldingItemCategory.Frame, ScaffoldingItemNames.Frame.Parser);
            tsSync = null;
        }
        else
        {
            tsSync?.AddFile(parserFramePath);
        }

        if (tsSync != null)
        {
            tsSync.AddFileIfExists(cgs.GetFrameFilePath(FrameFileNames.Copyright));
            tsSync.AddFileIfExists(cgs.GetFrameFilePath(FrameFileNames.Preface));
        }

        m_Compiler.Value.Compile(grammarFilePath);

        if (tsSync?.Timestamp is not null and var timestamp)
        {
            foreach (var filePath in m_IOService.Value.ModifiedFiles)
                File.SetLastWriteTimeUtc(filePath, timestamp.Value);
        }
    }
}

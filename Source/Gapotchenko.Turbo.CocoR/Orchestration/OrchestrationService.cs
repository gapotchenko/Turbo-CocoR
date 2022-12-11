using Gapotchenko.Turbo.CocoR.Compilation;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Scaffolding;
using System.Composition;

namespace Gapotchenko.Turbo.CocoR.Orchestration;

[Export(typeof(IOrchestrationService))]
[Shared]
sealed class OrchestrationService : IOrchestrationService
{
    [ImportingConstructor]
    public OrchestrationService(
        IOptionsService optionsService,
        Lazy<Compiler> compiler,
        Lazy<IScaffoldingService> scaffoldingService)
    {
        m_OptionsService = optionsService;
        m_Compiler = compiler;
        m_ScaffoldingService = scaffoldingService;
    }

    readonly IOptionsService m_OptionsService;
    readonly Lazy<Compiler> m_Compiler;
    readonly Lazy<IScaffoldingService> m_ScaffoldingService;

    public void CompileGrammar()
    {
        m_Compiler.Value.Compile(m_OptionsService.SourceFilePath);
    }

    public void Scaffold(string itemCategory, IEnumerable<string> itemNames)
    {
        bool quiet = m_OptionsService.Quiet;
        var scaffoldingService = m_ScaffoldingService.Value;

        foreach (var itemName in itemNames)
        {
            var templateName = scaffoldingService.CreateItem(itemCategory, itemName);
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
            m_ScaffoldingService.Value.CreateItem("grammar", grammarFilePath);

        // TODO

    }
}

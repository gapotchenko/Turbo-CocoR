namespace Gapotchenko.Turbo.CocoR.Orchestration;

interface IOrchestrationService
{
    void CompileGrammar();

    void Scaffold(string itemCategory, IEnumerable<string> itemNames);

    void CompileProjectGrammar(string grammarFilePath);
}

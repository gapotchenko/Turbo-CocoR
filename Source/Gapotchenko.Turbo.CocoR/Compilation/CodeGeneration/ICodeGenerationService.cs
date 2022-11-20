#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

interface ICodeGenerationService
{
    ICodeFrame OpenFrame(string fileName);

    ICodeFrame? TryOpenFrame(string fileName);

    ICodeWriter CreateWriter(string fileName);

    void GeneratePreface(ICodeWriter codeWriter);
}

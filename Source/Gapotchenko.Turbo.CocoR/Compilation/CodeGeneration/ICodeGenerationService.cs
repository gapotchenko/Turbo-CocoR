#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

interface ICodeGenerationService
{
    string GetFrameFilePath(string fileName);

    string? TryGetExplicitFrameFilePath(string? fileName);

    ICodeFrame OpenFrame(string fileName);

    ICodeFrame? TryOpenFrame(string fileName);

    ICodeWriter CreateWriter(string fileName);

    void GeneratePreface(ICodeWriter codeWriter);
}

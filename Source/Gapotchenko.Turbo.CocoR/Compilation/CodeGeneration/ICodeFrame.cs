#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

/// <summary>
/// Code frame is a template that is used for code generation.
/// </summary>
interface ICodeFrame : IDisposable
{
    void SkipPart(string name);
    void CopyPart(string name, TextWriter destination);
    void CopyRest(TextWriter destination);
}

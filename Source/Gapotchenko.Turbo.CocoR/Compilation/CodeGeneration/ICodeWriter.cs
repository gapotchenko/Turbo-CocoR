#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

interface ICodeWriter : IDisposable
{
    TextWriter Output { get; }

    int IndentLevel { get; set; }

    void Indent();

    void Unindent();
}

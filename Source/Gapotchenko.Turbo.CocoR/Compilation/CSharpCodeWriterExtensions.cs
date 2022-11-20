using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

namespace Gapotchenko.Turbo.CocoR.Compilation;

static class CSharpCodeWriterExtensions
{
    public static void BeginNamespace(this ICodeWriter codeWriter, ReadOnlySpan<char> name)
    {
        var output = codeWriter.Output;
        output.Write("namespace ");
        output.WriteLine(name);
        output.WriteLine('{');

        codeWriter.Indent();
    }

    public static void EndNamespace(this ICodeWriter codeWriter)
    {
        codeWriter.Unindent();

        codeWriter.Output.WriteLine('}');
    }
}

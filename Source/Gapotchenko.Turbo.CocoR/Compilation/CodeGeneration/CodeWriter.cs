namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

sealed class CodeWriter : ICodeWriter
{
    public CodeWriter(TextWriter textWriter)
    {
        m_Output = new IndentedTextWriter(textWriter, "\t");
    }

    readonly IndentedTextWriter m_Output;

    public void Dispose() => m_Output.Dispose();

    public TextWriter Output => m_Output;

    public int IndentLevel
    {
        get => m_Output.IndentLevel;
        set => m_Output.IndentLevel = value;
    }

    public void Indent() => ++IndentLevel;

    public void Unindent() => --IndentLevel;
}

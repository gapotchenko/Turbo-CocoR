using System.Text;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

sealed class IndentedTextWriter : TextWriter
{
    public IndentedTextWriter(TextWriter writer, string indent)
    {
        m_Output = writer;
        m_Indent = indent;
    }

    readonly string m_Indent;
    readonly TextWriter m_Output;

    int m_IndentLevel;
    bool m_IndentPending;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            m_Output.Dispose();
        base.Dispose(disposing);
    }

    public override void Close() => m_Output.Close();

    public override void Flush() => m_Output.Flush();

    public override IFormatProvider FormatProvider => base.FormatProvider;

    public override Encoding Encoding => m_Output.Encoding;

    [AllowNull]
    public override string NewLine
    {
        get => m_Output.NewLine;
        set => m_Output.NewLine = value;
    }

    public int IndentLevel
    {
        get => m_IndentLevel;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_IndentLevel = value;
        }
    }

    void WriteIndent()
    {
        if (m_IndentPending)
        {
            for (int i = 0; i < m_IndentLevel; i++)
                m_Output.Write(m_Indent);
            m_IndentPending = false;
        }
    }

    public override void Write(bool value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(char value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(object? value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(char[]? buffer)
    {
        WriteIndent();
        m_Output.Write(buffer);
    }

    public override void Write(double value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(int value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(long value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(float value)
    {
        WriteIndent();
        m_Output.Write(value);
    }

    public override void Write(string? s)
    {
        WriteIndent();
        m_Output.Write(s);
    }

    public override void Write(string format, params object?[] arg)
    {
        WriteIndent();
        m_Output.Write(format, arg);
    }

    public override void Write(string format, object? arg0)
    {
        WriteIndent();
        m_Output.Write(format, arg0);
    }

    public override void Write(string format, object? arg0, object? arg1)
    {
        WriteIndent();
        m_Output.Write(format, arg0, arg1);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        WriteIndent();
        m_Output.Write(buffer, index, count);
    }

    public override void WriteLine()
    {
        WriteIndent();
        m_Output.WriteLine();
        m_IndentPending = true;
    }

    public override void WriteLine(bool value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(char value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(object? value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(char[]? buffer)
    {
        WriteIndent();
        m_Output.WriteLine(buffer);
        m_IndentPending = true;
    }

    public override void WriteLine(double value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(int value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(long value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(float value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(string? s)
    {
        WriteIndent();
        m_Output.WriteLine(s);
        m_IndentPending = true;
    }

    public override void WriteLine(uint value)
    {
        WriteIndent();
        m_Output.WriteLine(value);
        m_IndentPending = true;
    }

    public override void WriteLine(string format, object? arg0)
    {
        WriteIndent();
        m_Output.WriteLine(format, arg0);
        m_IndentPending = true;
    }

    public override void WriteLine(string format, params object?[] arg)
    {
        WriteIndent();
        m_Output.WriteLine(format, arg);
        m_IndentPending = true;
    }

    public override void WriteLine(string format, object? arg0, object? arg1)
    {
        WriteIndent();
        m_Output.WriteLine(format, arg0, arg1);
        m_IndentPending = true;
    }

    public override void WriteLine(char[] buffer, int index, int count)
    {
        WriteIndent();
        m_Output.WriteLine(buffer, index, count);
        m_IndentPending = true;
    }
}

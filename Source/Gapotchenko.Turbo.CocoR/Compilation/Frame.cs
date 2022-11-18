#nullable enable

using Gapotchenko;

namespace Gapotchenko.Turbo.CocoR.Compilation;

sealed class Frame : IDisposable
{
    public Frame(string filePath)
    {
        m_FilePath = filePath;
        m_TextReader = File.OpenText(m_FilePath);
    }

    readonly string m_FilePath;
    readonly TextReader m_TextReader;

    public void Dispose() => m_TextReader.Dispose();

    public void SkipPart(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        CopyPartCore(name, null);
    }

    public void CopyPart(string? name, TextWriter destination)
    {
        ArgumentNullException.ThrowIfNull(nameof(destination));

        CopyPartCore(name, destination);
    }

    void CopyPartCore(string? name, TextWriter? output)
    {
        char startCh = default;
        int endOfStopString = 0;

        if (name != null)
        {
            startCh = name[0];
            endOfStopString = name.Length - 1;
        }

        int ch = m_TextReader.Read();
        while (ch != -1)
        {
            if (name != null && ch == startCh)
            {
                int i = 0;
                do
                {
                    if (i == endOfStopString)
                        return; // name[0..i] found
                    ch = m_TextReader.Read();
                    i++;
                } while (ch == name[i]);
                // name[0..i-1] found; continue with last read character
                output?.Write(name.AsSpan(0, i));
            }
            else
            {
                output?.Write((char)ch);
                ch = m_TextReader.Read();
            }
        }

        if (name != null)
            throw new Exception("Incomplete or corrupt frame file: " + m_FilePath);
    }
}

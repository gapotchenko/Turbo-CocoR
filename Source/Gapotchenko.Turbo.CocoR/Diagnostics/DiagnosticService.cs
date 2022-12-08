using System.Composition;

namespace Gapotchenko.Turbo.CocoR.Diagnostics;

sealed class DiagnosticService : IDiagnosticService
{
    DiagnosticService()
    {
    }

    [Export]
    public static IDiagnosticService Default { get; } = new DiagnosticService();

    public void Error(ReadOnlySpan<char> message, string? code) =>
        Write(Console.Error, "error", message, code);

    static readonly object m_SyncRoot = new();

    static void Write(TextWriter output, ReadOnlySpan<char> kind, ReadOnlySpan<char> message, string? code)
    {
        lock (m_SyncRoot)
        {
            output.Write(kind);
            if (!string.IsNullOrEmpty(code))
            {
                output.Write(' ');
                output.Write(code);
            }
            output.Write(": ");
            output.WriteLine(message.TrimEnd('.'));
        }
    }
}

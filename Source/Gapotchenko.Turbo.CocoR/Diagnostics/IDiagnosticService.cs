namespace Gapotchenko.Turbo.CocoR.Diagnostics;

interface IDiagnosticService
{
    void Error(ReadOnlySpan<char> message, string? code = null);
}

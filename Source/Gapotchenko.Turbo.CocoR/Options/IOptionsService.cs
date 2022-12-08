namespace Gapotchenko.Turbo.CocoR.Options;

#nullable enable

interface IOptionsService
{
    string SourceFilePath { get; }

    string? FramesDirectoryPath { get; }

    string OutputDirectoryPath { get; }

    string? Trace { get; }

    string? Namespace { get; }

    bool EmitLines { get; }

    string? Command { get; }

    IReadOnlyList<string> CommandArguments { get; }

    IReadOnlyDictionary<string, string> Properties { get; }

    bool NoLogo { get; }

    bool IntCall { get; }

    #region Calculated options

    bool HasSourceFile { get; }

    string SourceDirectoryPath { get; }

    bool KeepOldFiles { get; }

    #endregion

    void WriteUsage(TextWriter textWriter);
}

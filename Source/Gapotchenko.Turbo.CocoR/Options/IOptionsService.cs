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

    #region Calculated options

    bool HasSourceFile { get; }

    string SourceDirectoryPath { get; }

    bool KeepOldFiles { get; }

    #endregion

    void WriteUsage(TextWriter textWriter);
}

namespace Gapotchenko.Turbo.CocoR.Options;

interface IOptionsService
{
    string SourceFileName { get; }

    string? FramesDirectoryName { get; }

    string OutputDirectoryName { get; }

    string? Trace { get; }

    string? Namespace { get; }

    bool EmitLines { get; }

    #region Calculated options

    bool HasSourceFileName { get; }

    string SourceDirectoryName { get; }

    bool KeepOldFiles { get; }

    #endregion

    void WriteUsage(TextWriter textWriter);
}

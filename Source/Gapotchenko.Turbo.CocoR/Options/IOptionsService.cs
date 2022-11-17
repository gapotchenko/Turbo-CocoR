namespace Gapotchenko.Turbo.CocoR.Options;

interface IOptionsService
{
    #region Command-line options

    string SourceFileName { get; }

    string? FramesDirectoryName { get; }

    string OutputDirectoryName { get; }

    string? Trace { get; }

    string? Namespace { get; }

    bool EmitLines { get; }

    #endregion

    #region Calculated options

    bool HasSourceFileName { get; }

    string SourceDirectoryName { get; }

    bool KeepOldFiles { get; }

    #endregion
}

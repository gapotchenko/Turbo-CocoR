using Microsoft.Build.Utilities;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks;

public sealed class TcrSynchronizeTimestamp : Task
{
    /// <summary>
    /// Gets or sets the source files names.
    /// </summary>
    public string[] SourceFiles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the destination file names.
    /// </summary>
    public string[] DestinationFiles { get; set; } = Array.Empty<string>();

    public override bool Execute()
    {
        try
        {
            ExecuteCore();
            return true;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
            return false;
        }
    }

    void ExecuteCore()
    {
        var destinations = DestinationFiles.Where(File.Exists).ToList();
        if (destinations.Count == 0)
            return;

        var timestamp = DateTime.MinValue;

        foreach (var source in SourceFiles)
        {
            if (File.Exists(source))
            {
                var ts = File.GetLastWriteTimeUtc(source);
                if (ts > timestamp)
                    timestamp = ts;
            }
        }

        if (timestamp != DateTime.MinValue)
        {
            foreach (var destination in destinations)
                File.SetLastWriteTimeUtc(destination, timestamp);
        }
    }
}

using Microsoft.Build.Utilities;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks;

public sealed class TcrSynchronizeTimestamp : Task
{
    /// <summary>
    /// Gets or sets the source files names.
    /// </summary>
    public string[] Sources { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the destination file name.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

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
        if (!File.Exists(Destination))
            return;

        var timestamp = DateTime.MinValue;

        foreach (var fileName in Sources)
        {
            if (File.Exists(fileName))
            {
                var ts = File.GetLastWriteTimeUtc(fileName);
                if (ts > timestamp)
                    timestamp = ts;
            }
        }

        if (timestamp != DateTime.MinValue)
            File.SetLastWriteTimeUtc(Destination, timestamp);
    }
}

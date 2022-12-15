using Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Utilities;

/// <summary>
/// The base class for tasks built upon "turbo-coco" command-line tool.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class TcrToolTask : ToolTask
{
    public TcrToolTask()
    {
        // Use command interpreter to execute the command line.
        UseCommandProcessor = true;

        // Turn off echo for the command interpreter.
        EchoOff = true;
    }

    protected override string ToolName => "turbo-coco";

    protected override string GenerateFullPathToTool() =>
        Path.GetFullPath(Path.Combine(
            typeof(TcrToolTask).Assembly.Location,
            "..", // discard assembly file name
            @"..\..", // go to the root of the package
            "tools",
            ToolName));

    protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

    public override bool Execute()
    {
        try
        {
            return base.Execute();
        }
        catch (Exception e)
        {
            LogException(e);
            return false;
        }
    }

    protected void LogException(Exception e)
    {
        if (e.TryGetCode() is not null and var code)
            Log.LogError(null, code, null, null, 0, 0, 0, 0, "{0}", e.Message);
        else
            Log.LogErrorFromException(e);
    }
}

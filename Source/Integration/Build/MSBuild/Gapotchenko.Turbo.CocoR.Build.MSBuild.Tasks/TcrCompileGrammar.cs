using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks;

/// <summary>
/// Turbo Coco/R grammar compilation task for MSBuild.
/// </summary>
public sealed class TcrCompileGrammar : ToolTask
{
    protected override string ToolName => "turbo-coco";

    protected override string GenerateFullPathToTool() =>
        Path.GetFullPath(
            Path.Combine(
                typeof(TcrCompileGrammar).Assembly.Location,
                @"..\..\..\..",
                ToolName));

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_Grammar;

    [Required]
    public string Grammar
    {
        get => m_Grammar ?? throw new InvalidOperationException("Grammar is not set.");
        set => m_Grammar = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override bool Execute()
    {
        try
        {
            var grammarFilePath = Grammar;
            var grammarFile = new FileInfo(grammarFilePath);

            if (!grammarFile.Exists)
            {
                Log.LogError(null, "TCR0002", null, null, 0, 0, 0, 0, "Grammar file \"{0}\" does not exist.", grammarFilePath);
                return false;
            }

            bool grammarIsEmpty = grammarFile.Length == 0;
            if (!grammarIsEmpty)
            {
                // Use a text reader because the file can have a BOM but still be empty.
                using var tr = grammarFile.OpenText();
                grammarIsEmpty = tr.Read() == -1;
            }

            if (grammarIsEmpty)
            {
            }

            Log.LogMessage(MessageImportance.High, "Hello from MSBuild task");

            return true;
        }
        catch (Exception e)
        {
            Log.LogErrorFromException(e);
            return false;
        }
    }
}

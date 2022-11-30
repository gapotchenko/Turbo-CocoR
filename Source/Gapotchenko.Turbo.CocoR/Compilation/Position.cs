#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation;

/// <summary>
/// The position of a source code stretch (e.g. semantic action, resolver expressions).
/// </summary>
sealed class Position
{
    public Position(int begin, int end, int column, int line)
    {
        Begin = begin;
        End = end;
        Column = column;
        Line = line;
    }

    /// <summary>
    /// The start relative to the beginning of the file.
    /// </summary>
    public int Begin { get; }

    /// <summary>
    /// The end of stretch.
    /// </summary>
    public int End { get; }

    /// <summary>
    /// The column number of the start position.
    /// </summary>
    public int Column { get; }

    /// <summary>
    /// The line number of the start position.
    /// </summary>
    public int Line { get; }
}

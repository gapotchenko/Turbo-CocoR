using Gapotchenko.FX;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Options;

/*-------------------------------------------------------------------------
  Trace output options
  0 | A: prints the states of the scanner automaton
  1 | F: prints the First and Follow sets of all nonterminals
  2 | G: prints the syntax graph of the productions
  3 | I: traces the computation of the First sets
  4 | J: prints the sets associated with ANYs and synchronisation sets
  6 | S: prints the symbol table (terminals, nonterminals, pragmas)
  7 | X: prints a cross reference list of all syntax symbols
  8 | P: prints statistics about the Coco run
  
  Trace output can be switched on by the pragma
    $ { digit | letter }
  in the attributed grammar or as a command-line option
  -------------------------------------------------------------------------*/

sealed class OptionsService : IOptionsService
{
    public OptionsService(IReadOnlyList<string> args)
    {
        // Keeps the automatic compatibility with prior Coco/R versions for .NET.

        int argc = args.Count;
        for (int i = 0; i < argc; i++)
        {
            if (args[i] is "--namespace" or "-namespace" && i < argc - 1)
                Namespace = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "--frames" or "-frames" && i < argc - 1)
                FramesDirectoryName = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "--trace" or "-trace" && i < argc - 1)
                Trace = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "-o" or "--output" && i < argc - 1)
                m_OutputDirectoryName = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "--lines" or "-lines")
                EmitLines = true;
            else
            {
                if (m_SourceFileName != null)
                    throw new Exception("Multiple source files cannot be specified.");
                m_SourceFileName = args[i];
            }
        }

        #region Calculated options

        m_SourceDirectoryName = Path.GetDirectoryName(m_SourceFileName);
        if (KeepOldFiles = m_OutputDirectoryName == null)
            m_OutputDirectoryName = m_SourceDirectoryName;

        #endregion
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_SourceFileName;

    public string SourceFileName => m_SourceFileName ?? throw new Exception("Source file name is not specified.");

    public string? FramesDirectoryName { get; }

    public string? Trace { get; }

    public string? Namespace { get; }

    public bool EmitLines { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_OutputDirectoryName;

    public string OutputDirectoryName => m_OutputDirectoryName ?? throw new Exception("Output directory is unavailable.");

    #region Calculated options

    public bool HasSourceFileName => m_SourceFileName != null;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_SourceDirectoryName;

    public string SourceDirectoryName => m_SourceDirectoryName ?? throw new Exception("Source directory is unavailable.");

    public bool KeepOldFiles { get; }

    #endregion

    public void WriteUsage(TextWriter textWriter)
    {
        textWriter.WriteLine(
            "Options:{0}"
            + "  --namespace arg      Namespace name.{0}"
            + "  --frames arg         Frame files directory.{0}"
            + "  --trace arg          Trace string (see below).{0}"
            + "  -o [ --output ] arg  Output directory.{0}"
            + "  --lines              Emit lines.{0}"
            + "{0}"
            + "Valid characters in the trace string:{0}"
            + "  A  trace automaton{0}"
            + "  F  list first/follow sets{0}"
            + "  G  print syntax graph{0}"
            + "  I  trace computation of first sets{0}"
            + "  J  list ANY and SYNC sets{0}"
            + "  P  print statistics{0}"
            + "  S  list symbol table{0}"
            + "  X  list cross reference table{0}"
            + "{0}"
            + "Scanner.frame and Parser.frame files are needed in the ATG directory{0}"
            + "or in a directory specified by the --frames option.",
            Environment.NewLine);
    }
}


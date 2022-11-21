using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Deployment;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Options;

sealed class OptionsService : IOptionsService
{
    public OptionsService(
        IReadOnlyList<string> args,
        IProductInformationService productInformationService)
    {
        m_ProductInformationService = productInformationService;

        // Keep the compatibility with prior Coco/R versions for .NET circa 2011.

        bool help = false;

        int argc = args.Count;
        for (int i = 0; i < argc; i++)
        {
            if (args[i] is "--namespace" or "-namespace" && i < argc - 1)
                Namespace = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "--frames" or "-frames" && i < argc - 1)
                FramesDirectoryPath = Empty.Nullify(args[++i]);
            else if (args[i] is "--trace" or "-trace" && i < argc - 1)
                Trace = Empty.Nullify(args[++i].Trim());
            else if (args[i] is "-o" or "--output" && i < argc - 1)
                m_OutputDirectoryName = Empty.Nullify(args[++i]);
            else if (args[i] is "--lines" or "-lines")
                EmitLines = true;
            else if (args[i] is "-?" or "--help" or "/?" or "?")
                help = true;
            else
            {
                if (m_SourceFileName != null)
                    throw new Exception("Multiple source files cannot be specified.");
                m_SourceFileName = args[i];
            }
        }

        if (help)
            m_SourceFileName = null;

        #region Calculated options

        m_SourceDirectoryName = Path.GetDirectoryName(m_SourceFileName);
        if (KeepOldFiles = m_OutputDirectoryName == null)
            m_OutputDirectoryName = m_SourceDirectoryName;

        #endregion
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly IProductInformationService m_ProductInformationService;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_SourceFileName;

    public string SourceFilePath => m_SourceFileName ?? throw new Exception("Source file name is not specified.");

    public string? FramesDirectoryPath { get; }

    public string? Trace { get; }

    public string? Namespace { get; }

    public bool EmitLines { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_OutputDirectoryName;

    public string OutputDirectoryPath => m_OutputDirectoryName ?? throw new Exception("Output directory is unavailable.");

    #region Calculated options

    public bool HasSourceFile => m_SourceFileName != null;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string? m_SourceDirectoryName;

    public string SourceDirectoryPath => m_SourceDirectoryName ?? throw new Exception("Source directory is unavailable.");

    public bool KeepOldFiles { get; }

    #endregion

    public void WriteUsage(TextWriter textWriter)
    {
        WriteUsageCore(textWriter);
        textWriter.WriteLine();
        WriteUsageExtra(textWriter);
    }

    void WriteUsageCore(TextWriter textWriter)
    {
        string command = m_ProductInformationService.Command;
        textWriter.WriteLine(
            $"""
            Usage:
              {command} grammar.atg [options]
              {command} new frame <scanner|parser|preface> [options]
            """);
        textWriter.WriteLine();

        textWriter.WriteLine(
            """
            Options:
              -? [ --help ]        Display help.
              --namespace arg      Namespace name.
              --frames arg         Frame files directory.
              --trace arg          Trace string (see below).
              -o [ --output ] arg  Output directory.
              --lines              Emit lines.
            """);
    }

    static void WriteUsageExtra(TextWriter textWriter)
    {
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

        textWriter.WriteLine(
            """
            Valid characters in the trace string:
              A  trace automaton
              F  list first/follow sets
              G  print syntax graph
              I  trace computation of first sets
              J  list ANY and SYNC sets
              P  print statistics
              S  list symbol table
              X  list cross reference table
            
            Scanner.frame and Parser.frame files are needed in the ATG directory
            or in a directory specified by the --frames option.
            """);
    }
}

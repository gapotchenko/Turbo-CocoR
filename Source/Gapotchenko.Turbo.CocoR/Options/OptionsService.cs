using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Deployment;
using Gapotchenko.Turbo.CocoR.Framework.Diagnostics;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Options;

sealed class OptionsService : IOptionsService
{
    public OptionsService(IReadOnlyList<string> args)
    {
        m_ProductInformationService = ProductInformationService.Default;

        // Keep the compatibility with prior Coco/R versions for .NET circa 2011.

        bool help = false;
        var positionalOptions = new List<string>();
        string? outputDirectoryPath = null;
        bool force = false;

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
                outputDirectoryPath = Empty.Nullify(args[++i]);
            else if (args[i] is "--lines" or "-lines")
                EmitLines = true;
            else if (args[i] is "-f" or "--force")
                force = true;
            else if (args[i] is "--property" && i < argc - 2)
                m_Properties[args[++i]] = args[++i];
            else if (args[i] is "-?" or "--help" or "/?" or "?")
                help = true;
            else
                positionalOptions.Add(args[i]);
        }

        if (help)
            positionalOptions.Clear();

        if (positionalOptions.Count != 0)
        {
            string option = positionalOptions[0];
            if (option == "new")
            {
                Command = option;
                m_CommandArguments = positionalOptions.Skip(1).ToList();
            }
            else
            {
                if (positionalOptions.Count > 1)
                    throw new Exception("Multiple source files cannot be specified.").Categorize("TCR0002");
                m_SourceFilePath = option;
            }
        }

        #region Calculated options

        m_SourceDirectoryPath = Path.GetDirectoryName(m_SourceFilePath);

        if (outputDirectoryPath == null)
        {
            KeepOldFiles = !force;
            outputDirectoryPath = m_SourceDirectoryPath ?? ".";
        }
        OutputDirectoryPath = outputDirectoryPath;

        #endregion
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly IProductInformationService m_ProductInformationService;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly string? m_SourceFilePath;

    public string SourceFilePath => m_SourceFilePath ?? throw new Exception("Source file path is not specified.");

    public string? FramesDirectoryPath { get; }

    public string? Trace { get; }

    public string? Namespace { get; }

    public bool EmitLines { get; }

    public string OutputDirectoryPath { get; }

    public string? Command { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly IReadOnlyList<string>? m_CommandArguments;

    public IReadOnlyList<string> CommandArguments => m_CommandArguments ?? throw new Exception("Command arguments are unavailable.");

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly Dictionary<string, string> m_Properties = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> Properties => m_Properties;

    #region Calculated options

    public bool HasSourceFile => m_SourceFilePath != null;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    readonly string? m_SourceDirectoryPath;

    public string SourceDirectoryPath => m_SourceDirectoryPath ?? throw new Exception("Source directory is unavailable.");

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
              {command} new grammar grammar.atg [options]
              {command} new frame <scanner|parser|preface> [options]
            """);
        textWriter.WriteLine();

        textWriter.WriteLine(
            $"""
            Options:
              -? [ --help ]        Display help.
              --namespace arg      Namespace name.
              --frames arg         Frame files directory.
              --trace arg          Trace string (see below).
              -o [ --output ] arg  Output directory.
              --lines              Emit lines.
            """);

#if TODO
        textWriter.WriteLine(
            $"""
            Options:
              -? [ --help ]               Display help.
              --namespace arg             Namespace name.
              --frames arg                Frame files directory.
              --trace arg                 Trace string (see below).
              -o [ --output ] arg         Output directory.
              --lines                     Emit lines.
              --lang arg (=auto)          The programming language to use for code
                                          generation. Possible values: C#, auto.
              --lang-version arg (=auto)  The version of a programming language to use for
                                          code generation.
              --lang-features arg         The list of programming language features to use
                                          for code generation (see below).
              --compatibility arg         The compatibility string specifies the name and
                                          version of a Coco/R tool to be compatible with.
                                          For example: "{m_ProductInformationService.Name} {m_ProductInformationService.SignificantVersion}"
            """);
#endif
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
            Valid characters in the trace string (--trace option):
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

#if TODO
            """
            Valid programming language features to use for code generation (--lang-features
            option):
              C#  nullable, no-nullable
            """
#endif
    }
}

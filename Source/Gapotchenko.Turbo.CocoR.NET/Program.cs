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

using Gapotchenko.Turbo.CocoR.NET.Grammar;

namespace Gapotchenko.Turbo.CocoR.NET;

static class Program
{
    public static int Main(string[] arg)
    {
        Console.WriteLine("Turbo Coco/R .NET 2022.1.1");
        string srcName = null, nsName = null, frameDir = null, ddtString = null,
        outDir = null;
        bool emitLines = false;
        int retVal = 1;
        for (int i = 0; i < arg.Length; i++)
        {
            if (arg[i] == "-namespace" && i < arg.Length - 1) nsName = arg[++i].Trim();
            else if (arg[i] == "-frames" && i < arg.Length - 1) frameDir = arg[++i].Trim();
            else if (arg[i] == "-trace" && i < arg.Length - 1) ddtString = arg[++i].Trim();
            else if (arg[i] == "-o" && i < arg.Length - 1) outDir = arg[++i].Trim();
            else if (arg[i] == "-lines") emitLines = true;
            else srcName = arg[i];
        }
        if (arg.Length > 0 && srcName != null)
        {
            Console.WriteLine();
            try
            {
                string srcDir = Path.GetDirectoryName(srcName);
                bool keepOldFiles = outDir == null;
                outDir ??= srcDir;

                string traceFilePath = Path.Combine(outDir, "Trace.txt");

                var scanner = new Scanner(srcName);
                var parser = new Parser(scanner)
                {
                    trace = new StreamWriter(new FileStream(traceFilePath, FileMode.Create))
                };

                parser.tab = new Tab(parser) { KeepOldFiles = keepOldFiles };
                parser.dfa = new DFA(parser);
                parser.pgen = new ParserGen(parser);

                parser.tab.srcName = srcName;
                parser.tab.srcDir = srcDir;
                parser.tab.nsName = nsName;
                parser.tab.frameDir = frameDir;
                parser.tab.outDir = outDir;
                parser.tab.emitLines = emitLines;
                if (ddtString != null) parser.tab.SetDDT(ddtString);

                parser.Parse();

                parser.trace.Close();
                var f = new FileInfo(traceFilePath);
                if (f.Length == 0)
                    f.Delete();
                else
                    Console.WriteLine("Trace output has been written to \"{0}\" file.", traceFilePath);
                Console.WriteLine("{0} errors detected.", parser.errors.count);
                if (parser.errors.count == 0)
                    retVal = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine("-- " + e.Message);
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Usage: turbo-coco-dotnet grammar.atg <option>{0}"
                              + "{0}"
                              + "Options:{0}"
                              + "  -namespace arg  Namespace name.{0}"
                              + "  -frames arg     Frame files directory.{0}"
                              + "  -trace arg      Trace string (see below).{0}"
                              + "  -o arg          Output directory.{0}"
                              + "  -lines{0}"
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
                              + "\"Scanner.frame\" and \"Parser.frame\" files needed in ATG directory{0}"
                              + "or in a directory specified by the -frames option.",
                              Environment.NewLine);
        }
        return retVal;
    }
}

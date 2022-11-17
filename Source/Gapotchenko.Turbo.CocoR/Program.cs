using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.NET.Grammar;
using Gapotchenko.Turbo.CocoR.Options;

namespace Gapotchenko.Turbo.CocoR.NET;

static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            Run(args);
            return 0;
        }
        catch (ProgramExitException e)
        {
            return e.ExitCode;
        }
        catch (Exception e)
        {
            var output = Console.Error;
            output.Write("Error: ");
            output.WriteLine(e.Message);
            return 1;
        }
    }

    static void Run(IReadOnlyList<string> args)
    {
        Console.WriteLine("Turbo Coco/R 2022.1.1");

        var optionsService = new OptionsService(args);

        if (args.Count > 0 && optionsService.HasSourceFileName)
        {
            Console.WriteLine();
            try
            {
                string traceFilePath = Path.Combine(optionsService.OutputDirectoryName, "Trace.txt");

                var scanner = new Scanner(optionsService.SourceFileName);
                var parser = new Parser(scanner)
                {
                    trace = new StreamWriter(new FileStream(traceFilePath, FileMode.Create))
                };

                parser.tab = new Tab(parser) { KeepOldFiles = optionsService.KeepOldFiles };
                parser.dfa = new DFA(parser);
                parser.pgen = new ParserGen(parser);

                parser.tab.srcName = optionsService.SourceFileName;
                parser.tab.srcDir = optionsService.SourceDirectoryName;
                parser.tab.nsName = optionsService.Namespace;
                parser.tab.frameDir = optionsService.FramesDirectoryName;
                parser.tab.outDir = optionsService.OutputDirectoryName;
                parser.tab.emitLines = optionsService.EmitLines;
                if (optionsService.Trace is not null and var trace)
                    parser.tab.SetTrace(trace);

                parser.Parse();

                parser.trace.Close();
                var f = new FileInfo(traceFilePath);
                if (f.Length == 0)
                    f.Delete();
                else
                    Console.WriteLine("Trace output has been written to \"{0}\" file.", traceFilePath);
                Console.WriteLine("{0} errors detected.", parser.errors.count);
                if (parser.errors.count != 0)
                    throw new ProgramExitException(1);
            }
            catch (Exception e)
            {
                Console.WriteLine("-- " + e.Message);
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Usage: turbo-coco grammar.atg [options]");
            Console.WriteLine();
            optionsService.WriteUsage(Console.Out);
            throw new ProgramExitException(1);
        }
    }
}

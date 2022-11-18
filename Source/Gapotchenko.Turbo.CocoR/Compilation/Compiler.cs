using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation.Grammar;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

namespace Gapotchenko.Turbo.CocoR.Compilation;

[Export]
sealed class Compiler
{
    [ImportingConstructor]
    public Compiler(IOptionsService optionsService)
    {
        m_OptionsService = optionsService;
    }

    readonly IOptionsService m_OptionsService;

    public void Compile()
    {
        bool keepTraceFile = false;
        int errorsCount = 0;

        string traceFilePath = Path.Combine(m_OptionsService.OutputDirectoryName, "Trace.txt");
        using (var traceFile = File.CreateText(traceFilePath))
        {
            errorsCount = CompileCore(traceFile);
            keepTraceFile = traceFile.BaseStream.Length != 0;
        }

        if (keepTraceFile)
            Console.WriteLine("Trace output has been written to \"{0}\" file.", traceFilePath);
        else
            File.Delete(traceFilePath);

        Console.WriteLine("{0} errors detected.", errorsCount);
        if (errorsCount != 0)
            throw new ProgramExitException(1);
    }

    int CompileCore(TextWriter traceTextWriter)
    {
        var scanner = new Scanner(m_OptionsService.SourceFileName);
        var parser = new Parser(scanner)
        {
            trace = traceTextWriter
        };

        parser.tab = new Tab(parser) { KeepOldFiles = m_OptionsService.KeepOldFiles };
        parser.dfa = new DFA(parser);
        parser.pgen = new ParserGen(parser);

        parser.tab.srcName = m_OptionsService.SourceFileName;
        parser.tab.srcDir = m_OptionsService.SourceDirectoryName;
        parser.tab.nsName = m_OptionsService.Namespace;
        parser.tab.frameDir = m_OptionsService.FramesDirectoryName;
        parser.tab.outDir = m_OptionsService.OutputDirectoryName;
        parser.tab.emitLines = m_OptionsService.EmitLines;
        if (m_OptionsService.Trace is not null and var trace)
            parser.tab.SetTrace(trace);

        parser.Parse();

        return parser.errors.count;
    }
}

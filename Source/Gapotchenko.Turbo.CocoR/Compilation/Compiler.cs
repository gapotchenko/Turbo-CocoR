using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;
using Gapotchenko.Turbo.CocoR.Compilation.Grammar;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation;

[Export]
sealed class Compiler
{
    [ImportingConstructor]
    public Compiler(IOptionsService optionsService, ICodeGenerationService codeGenerationService)
    {
        m_OptionsService = optionsService;
        m_CodeGenerationService = codeGenerationService;
    }

    readonly IOptionsService m_OptionsService;
    readonly ICodeGenerationService m_CodeGenerationService;

    public void Compile()
    {
        int errorsCount;

        string traceFilePath = Path.Combine(m_OptionsService.OutputDirectoryPath, "Trace.txt");
        bool keepTraceFile = false;
        try
        {
            using var traceFile = File.CreateText(traceFilePath);
            try
            {
                errorsCount = CompileCore(traceFile);
            }
            finally
            {
                keepTraceFile = traceFile.BaseStream.Length != 0;
            }
        }
        finally
        {
            if (keepTraceFile)
                Console.WriteLine("Trace output has been written to \"{0}\" file.", traceFilePath);
            else
                File.Delete(traceFilePath);
        }

        Console.WriteLine("{0} errors detected.", errorsCount);
        if (errorsCount != 0)
            throw new ProgramExitException(1);
    }

    int CompileCore(TextWriter traceTextWriter)
    {
        var scanner = new Scanner(m_OptionsService.SourceFilePath);
        var parser = new Parser(scanner)
        {
            trace = traceTextWriter
        };

        parser.tab = new Tab(parser)
        {
            CodeGenerationService = m_CodeGenerationService
        };
        parser.dfa = new DFA(parser);
        parser.pgen = new ParserGen(parser);

        parser.tab.srcName = m_OptionsService.SourceFilePath;
        parser.tab.nsName = m_OptionsService.Namespace;
        parser.tab.emitLines = m_OptionsService.EmitLines;
        if (m_OptionsService.Trace is not null and var trace)
            parser.tab.SetTrace(trace);

        parser.Parse();

        return parser.errors.count;
    }
}

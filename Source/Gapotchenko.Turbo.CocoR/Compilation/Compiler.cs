using Gapotchenko.FX;
using Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;
using Gapotchenko.Turbo.CocoR.Compilation.Grammar;
using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation;

[Export]
[Shared]
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

    public void Compile(string grammarFilePath)
    {
        int errorsCount;

        string traceFilePath = Path.Combine(m_OptionsService.OutputDirectoryPath, "Trace.txt");
        bool keepTraceFile = false;
        try
        {
            using var traceFile = File.CreateText(traceFilePath);
            try
            {
                errorsCount = CompileCore(grammarFilePath, traceFile);
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

        if (!m_OptionsService.Quiet)
            Console.WriteLine("{0} errors detected.", errorsCount);
        if (errorsCount != 0)
            throw new ProgramExitException(1);
    }

    int CompileCore(string grammarFilePath, TextWriter traceTextWriter)
    {
        var scanner = new Scanner(grammarFilePath);
        var parser = new Parser(scanner)
        {
            trace = traceTextWriter
        };

        parser.errors.FileName = grammarFilePath;

        parser.tab = new Tab(parser)
        {
            CodeGenerationService = m_CodeGenerationService,
            Quiet = m_OptionsService.Quiet
        };
        parser.dfa = new DFA(parser);
        parser.pgen = new ParserGen(parser);

        parser.tab.srcName = grammarFilePath;
        parser.tab.nsName = m_OptionsService.Namespace;
        parser.tab.emitLines = m_OptionsService.EmitLines;
        if (m_OptionsService.Trace is not null and var trace)
            parser.tab.SetTrace(trace);

        parser.Parse();

        return parser.errors.count;
    }
}

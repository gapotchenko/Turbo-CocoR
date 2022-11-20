using Gapotchenko.Turbo.CocoR.IO;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Scaffolding;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

[Export(typeof(ICodeGenerationService))]
sealed class CodeGenerationService : ICodeGenerationService
{
    [ImportingConstructor]
    public CodeGenerationService(
        IOptionsService optionsService,
        IIOService ioService,
        IScaffoldingService scaffoldingService)
    {
        m_OptionsService = optionsService;
        m_IOService = ioService;
        m_ScaffoldingService = scaffoldingService;
    }

    readonly IOptionsService m_OptionsService;
    readonly IIOService m_IOService;
    readonly IScaffoldingService m_ScaffoldingService;

    public ICodeFrame? TryOpenFrame(string fileName)
    {
        string filePath = Path.Combine(m_OptionsService.SourceDirectoryPath, fileName);
        if (!File.Exists(filePath) && m_OptionsService.FramesDirectoryPath is not null and var frameDir)
            filePath = Path.Combine(frameDir, fileName);
        if (File.Exists(filePath))
            return new CodeFrame(File.OpenText(filePath), filePath);

        bool useTemplate =
            fileName switch
            {
                "Preface.frame" => true,
                _ => false
            };

        if (useTemplate)
        {
            var template = m_ScaffoldingService.TryOpenTemplate(fileName);
            if (template != null)
                return new CodeFrame(template, fileName);
        }

        return null;
    }

    public ICodeFrame OpenFrame(string fileName) =>
        TryOpenFrame(fileName) ??
        throw new Exception("Cannot find : " + fileName);

    public ICodeWriter CreateWriter(string fileName)
    {
        string filePath = Path.Combine(m_OptionsService.OutputDirectoryPath, fileName);
        m_IOService.CreateFileBackup(filePath);
        return new CodeWriter(File.CreateText(filePath));
    }

    public void GeneratePreface(ICodeWriter codeWriter)
    {
        using (var frame = TryOpenFrame("Preface.frame"))
            frame?.CopyRest(codeWriter.Output);

        using (var frame = TryOpenFrame("Copyright.frame"))
            frame?.CopyRest(codeWriter.Output);
    }
}

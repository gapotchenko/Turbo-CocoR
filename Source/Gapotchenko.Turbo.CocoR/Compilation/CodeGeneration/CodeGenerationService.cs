using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

[Export(typeof(ICodeGenerationService))]
sealed class CodeGenerationService : ICodeGenerationService
{
    [ImportingConstructor]
    public CodeGenerationService(IOptionsService optionsService)
    {
        m_OptionsService = optionsService;
    }

    readonly IOptionsService m_OptionsService;

    public ICodeFrame? TryOpenFrame(string fileName)
    {
        string filePath = Path.Combine(m_OptionsService.SourceDirectoryName, fileName);
        if (!File.Exists(filePath) && m_OptionsService.FramesDirectoryName is not null and var frameDir)
            filePath = Path.Combine(frameDir, fileName);
        if (!File.Exists(filePath))
            return null;

        return new CodeFrame(filePath);
    }

    public ICodeFrame OpenFrame(string fileName) =>
        TryOpenFrame(fileName) ??
        throw new Exception("Cannot find : " + fileName);

    public ICodeWriter CreateWriter(string fileName)
    {
        string filePath = Path.Combine(m_OptionsService.OutputDirectoryName, fileName);

        if (m_OptionsService.KeepOldFiles)
        {
            if (File.Exists(filePath))
                File.Copy(filePath, filePath + ".old", true);
        }

        return new CodeWriter(File.CreateText(filePath));
    }

    public void GenerateCopyright(ICodeWriter codeWriter)
    {
        using var frame = TryOpenFrame("Copyright.frame");
        if (frame != null)
            frame.CopyRest(codeWriter.Output);
    }
}

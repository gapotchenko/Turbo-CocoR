using Gapotchenko.Turbo.CocoR.IO;
using Gapotchenko.Turbo.CocoR.Options;
using Gapotchenko.Turbo.CocoR.Scaffolding;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.CodeGeneration;

[Export(typeof(ICodeGenerationService))]
[Shared]
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

    public string GetFrameFilePath(string fileName) => GetFrameFilePathExplained(fileName).Path;

    enum FrameFilePathExplanation
    {
        /// <summary>
        /// The frame file path was specified explicitly.
        /// </summary>
        Explicit,

        /// <summary>
        /// The frame file path comes from the source directory.
        /// </summary>
        SourceDirectory,

        /// <summary>
        /// The frame file path comes from the frame directory.
        /// </summary>
        FrameDirectory
    }

    (string Path, FrameFilePathExplanation Explanation) GetFrameFilePathExplained(string fileName)
    {
        string? filePath = TryGetExplicitFrameFilePath(fileName);
        if (filePath != null)
            return (filePath, FrameFilePathExplanation.Explicit);

        filePath = Path.Combine(m_OptionsService.SourceDirectoryPath, fileName);
        var explanation = FrameFilePathExplanation.SourceDirectory;

        if (!File.Exists(filePath) && m_OptionsService.FramesDirectoryPath is not null and var frameDir)
        {
            filePath = Path.Combine(frameDir, fileName);
            explanation = FrameFilePathExplanation.FrameDirectory;
        }

        return (filePath, explanation);
    }

    public string? TryGetExplicitFrameFilePath(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (!string.IsNullOrEmpty(Path.GetDirectoryName(fileName)))
            return null;

        string propertyName = Path.GetFileNameWithoutExtension(fileName) + "Frame";
        if (m_OptionsService.Properties.TryGetValue(propertyName, out var value))
            return value;

        return null;
    }

    public ICodeFrame? TryOpenFrame(string fileName)
    {
        var (filePath, filePathExplanation) = GetFrameFilePathExplained(fileName);

        if (File.Exists(filePath))
            return new CodeFrame(File.OpenText(filePath), filePath);

        if (filePathExplanation == FrameFilePathExplanation.Explicit)
        {
            // If the frame file is specified explicitly then we cannot use the templates because they are implicit.
            return null;
        }

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

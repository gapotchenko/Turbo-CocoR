using Gapotchenko.Turbo.CocoR.Options;
using System.Composition;

#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.Frames;

[Export(typeof(IFramesService))]
sealed class FramesService : IFramesService
{
    [ImportingConstructor]
    public FramesService(IOptionsService optionsService)
    {
        m_OptionsService = optionsService;
    }

    readonly IOptionsService m_OptionsService;

    public IFrame OpenFrame(string fileName)
    {
        throw new NotImplementedException();
    }

    public IFrame? TryOpenFrame(string fileName)
    {
        string filePath = Path.Combine(m_OptionsService.SourceDirectoryName, fileName);
        if (!File.Exists(filePath) && m_OptionsService.FramesDirectoryName is not null and var frameDir)
            filePath = Path.Combine(frameDir, fileName);
        if (!File.Exists(filePath))
            return null;

        return new Frame(filePath);
    }
}

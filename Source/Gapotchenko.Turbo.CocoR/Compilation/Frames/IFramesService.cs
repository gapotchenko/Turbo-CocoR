#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.Frames;

interface IFramesService
{
    IFrame OpenFrame(string fileName);

    IFrame? TryOpenFrame(string fileName);
}

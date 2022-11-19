#nullable enable

namespace Gapotchenko.Turbo.CocoR.Compilation.Frames;

interface IFrame : IDisposable
{
    void SkipPart(string name);
    void CopyPart(string name, TextWriter destination);
    void CopyRest(TextWriter destination);
}

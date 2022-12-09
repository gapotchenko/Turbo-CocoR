using Microsoft.Build.Utilities;

namespace Gapotchenko.Turbo.CocoR.Build.MSBuild.Tasks.Utils;

sealed class CommandLineBuilderEx : CommandLineBuilder
{
    public void AppendParameter(string value)
    {
        AppendSpaceIfNotEmpty();
        AppendTextWithQuoting(value);
    }

    public void AppendParameter(string name, string value)
    {
        AppendSwitch(name);
        AppendParameter(value);
    }

    public void AppendProperty(string name, string value)
    {
        AppendSwitch("-p");
        AppendParameter(name);
        AppendParameter(value);
    }
}

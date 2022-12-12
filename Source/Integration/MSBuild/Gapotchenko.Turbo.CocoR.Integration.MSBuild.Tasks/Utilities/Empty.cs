namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Utilities;

static class Empty
{
    public static string? NullifyWhiteSpace(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return value;
    }
}

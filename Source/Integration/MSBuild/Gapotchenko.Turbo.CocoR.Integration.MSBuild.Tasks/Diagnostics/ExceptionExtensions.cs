namespace Gapotchenko.Turbo.CocoR.Integration.MSBuild.Tasks.Diagnostics;

static class ExceptionExtensions
{
    static readonly object CodeKey = new();

    public static Exception Categorize(this Exception exception, string code)
    {
        exception.Data[CodeKey] = code;
        return exception;
    }

    public static string? TryGetCode(this Exception exception) => exception.Data[CodeKey] as string;
}

# Turbo Coco/R Project Integration

`Gapotchenko.Turbo.CocoR.Build` NuGet package allows you to integrate Turbo Coco/R at the project level.
The integration currently supports MSBuild projects that are used by languages like C#, VB.NET and F#.

The MSBuild project must be in new [SDK-style format](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview).
While that project format is mainly associated with newer target frameworks such as .NET Core and .NET, it is perfectly usable for .NET Framework as well.

{% tip %}

**Tip:** The new project format is a good thing, and if you are not converted yet then you should strongly consider it.
Tools like [dotnet-migrate-2019](https://github.com/hvanbakel/CsprojToVs2017) will help you with that.

{% endtip %}

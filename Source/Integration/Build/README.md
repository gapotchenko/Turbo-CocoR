# Turbo Coco/R Build Integration

`Gapotchenko.Turbo.CocoR.Build` NuGet package allows you to integrate [Turbo Coco/R](https://github.com/gapotchenko/Turbo-CocoR) at the project level.
The integration currently supports MSBuild projects that are used by languages like C#, VB.NET and F#.

## Getting Started

1. Add [`Gapotchenko.Turbo.CocoR.Build`](https://www.nuget.org/packages/Gapotchenko.Turbo.CocoR.Build) NuGet package to your MSBuild project
2. Every attributed grammar file with `.atg` extension will be automatically processed with `turbo-coco` tool during the build using the following rules:
    - If the `.atg` file is empty then it will be automatically filled with a starter Turbo Coco/R grammar for your convenience
    - Frame files will be automatically created if they do not exist yet
    - The scanner and parser files will be automatically generated for the corresponding `.atg` file
3. That's it!

Project integration significantly improves the development round-trip times comparing to the traditional approach using `turbo-coco` command-line interface.

## Requirements

- Turbo Coco/R requires .NET 7.0+ to run
- The MSBuild project MUST be in a new [SDK-style format](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview).
  While that project format is mainly associated with newer target frameworks such as .NET Core and .NET, it is also perfectly usable for .NET Framework
  
  (**Tip:** The new MSBuild project format is a good thing, and if you are not converted yet then you should strongly consider it.
  Tools like [dotnet migrate-2019](https://github.com/hvanbakel/CsprojToVs2017) may help you with that)

Please note that:
- The produced source files are not subject to any requirements and can work anywhere
- `Gapotchenko.Turbo.CocoR.Build` NuGet package can be used in a project targeting not only .NET 7.0+ but any framework without restrictions
- `Gapotchenko.Turbo.CocoR.Build` NuGet package does not rely on the installed `turbo-coco` command-line tool.
  Instead, the package comes with the tool bundled inside.
  This allows you to use the specific version of Turbo Coco/R just by selecting the corresponding version of `Gapotchenko.Turbo.CocoR.Build` NuGet package in your project

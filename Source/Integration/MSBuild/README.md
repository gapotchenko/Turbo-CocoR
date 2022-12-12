# Turbo Coco/R MSBuild Integration

`Gapotchenko.Turbo.CocoR.Integration.MSBuild` NuGet package allows you to integrate [Turbo Coco/R](https://github.com/gapotchenko/Turbo-CocoR) at the MSBuild project level.
The integration currently supports C# MSBuild projects.

## Getting Started

1. Add [`Gapotchenko.Turbo.CocoR.Integration.MSBuild`](https://www.nuget.org/packages/Gapotchenko.Turbo.CocoR.Integration.MSBuild) NuGet package to your MSBuild project
2. Every attributed grammar file with `.atg` extension will be automatically processed by Turbo Coco/R during the build using the following rules:
    - If the `.atg` file is empty then it will be automatically filled with a starter grammar for your convenience
    - Frame files will be automatically created if they do not exist yet
    - The scanner and parser files will be generated automatically on every grammar modification

Project integration significantly reduces development time compared to the traditional approach with the manual usage of `turbo-coco` command-line tool.

## Requirements

- Turbo Coco/R requires .NET 7.0+ to run
- The MSBuild project MUST be in a new [SDK-style format](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview).
  While that project format is mainly associated with newer target frameworks such as .NET Core and .NET, it is also perfectly usable for .NET Framework.
  
  (**Tip:** The new MSBuild project format is a good thing, and if you are not converted yet then you should strongly consider it.
  Tools like [dotnet migrate-2019](https://github.com/hvanbakel/CsprojToVs2017) may help you with that)

Please note that:
- The produced source files are not subject to any requirements and can work anywhere
- `Gapotchenko.Turbo.CocoR.Integration.MSBuild` NuGet package can be used in a project targeting not only .NET 7.0+ but any other framework without restrictions
- `Gapotchenko.Turbo.CocoR.Integration.MSBuild` NuGet package does not rely on the installed `turbo-coco` command-line tool.
  Instead, the package comes with the tool bundled inside.
  This allows you to use the specific version of Turbo Coco/R just by selecting the corresponding version of `Gapotchenko.Turbo.CocoR.Build` NuGet package in your project

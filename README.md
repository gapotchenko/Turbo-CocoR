# Turbo Coco/R
Turbo Coco/R is a compile-time compiler generator which takes an attributed grammar of a source language and generates a scanner and a parser for this language. It is based on the de-facto Coco/R standard and extends it to be more useful in commercial settings.

## Features

- Follows the baseline [Coco/R standard](https://ssw.jku.at/Research/Projects/Coco/) with some extensions
- Multilingual support: C#, other languages are coming
- Ready for consumption: available in a handy regularly-updated prebuilt package

## Basics

Turbo Coco/R is very similar to once popular `lex` and `yacc` tools and their open-source `flex` and `bison` counterparts.
The main distinction of Turbo Coco/R is that it provides the support for multiple programming languages and generates both a scanner and a parser from a provided grammar file.

The produced scanners and parsers are tiny, do not have external dependendencies and thus can be embedded into any project at the source level.

The scanner works as a deterministic finite automaton.
The parser uses recursive descent.
A multi-symbol lookahead or semantic checks can resolve LL(1) conflicts. Thus the class of accepted grammars is LL(k) for an arbitrary k.

## Getting Started

1. Install Turbo Coco/R tool using .NET package manager:

   ``` sh
   > dotnet tool install Gapotchenko.Turbo.CocoR --global
   ```

2. Create your first attributed grammar file:

   ``` sh
   > turbo-coco new grammar MyLang.atg
   ```

3. Create the customizable frame files for a scanner and parser:

   ``` sh
   > turbo-coco new frame scanner parser
   ```

   A frame file defines the basic code structure of a generated file.

Now you can generate the actual scanner and parser source files for your grammar:

``` sh
> turbo-coco MyLang.atg
```

Once generated, the files are ready to be compiled as a part of your project.

For further guidance, see the [examples](https://github.com/gapotchenko/Turbo-CocoR/tree/main/Examples).

## Requirements

- Turbo Coco/R tool requires .NET SDK 7.0+
- The produced source files are not subject to any requirements and can work anywhere

## Licensing

Turbo Coco/R is licensed under GNU General Public License v2.0.

If not otherwise stated, any source code generated by Turbo Coco/R
(other than Turbo Coco/R itself) does not fall under the GNU General
Public License.

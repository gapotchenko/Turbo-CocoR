# RadCalc

RadCalc is a C# sample demonstrating the rapid application development (RAD) concept by using Turbo Coco/R [MSBuild project integration](https://github.com/gapotchenko/Turbo-CocoR/tree/main/Source/Integration/MSBuild).

The resulting application asks a user to enter a math expression and then calculates the result.
The sample has a simple single-pass architecture: the result is being calculated as the parsing progresses.

Supported math operators:

- Addition: `+`
- Subtraction: `-`
- Multiplication: `*`
- Division: `/`

Supported math functions:

- `abs(x)`: gets absolute value of a number

Operations are grouped according to their natural math order but can be prioritized by brackets, like so:

```
(10 + 20) * 3
```

## Development

If you want to play with the grammar, you can modify the `.atg` or a `.frame` file.

Thanks to the MSBuild integration, the updated Turbo Coco/R scanner and parser files are generated automatically during the build (`*.gen.cs`) whenever modification of a grammar file is detected.
Such approach allows you to significantly reduce the development time compared to the `turbo-coco` command-line approach.

# RadCalc

RadCalc is a C# sample demonstrating the use of Turbo Coco/R MSBuild integration.
It asks a user to enter a math expression and then calculates the result.
The sample has a simple single-pass architecture: the result is being calculated as the parsing progresses.

Supported math operators:

- Addition: `+`
- Subtraction: `-`
- Multiplication: `*`
- Division: `/`

Supported math functions:

- `abs(x)`: gets absolute value of a number.

Operations are grouped according to their natural math order but can be prioritized by brackets, like so:

```
(10 + 20) * 3
```

If you want to play with the grammar, you can modify the `.atg` or a `.frame` file.
Thanks to the MSBuild integration, the updated Turbo Coco/R scanner and parser files are generated automatically during the build (`*.gen.cs`) whenever modification of a grammar file is detected.

Turbo Coco/R MSBuild integration allows you to significantly reduce the development time compared to the traditional approach with the manual invocation of `turbo-coco` command-line tool.

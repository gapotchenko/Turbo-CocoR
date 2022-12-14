# Calc

Calc is the easiest Turbo Coco/R sample for C#.
It asks a user to enter a math expression and then calculates the result.
The sample demonstrates the simplest single-pass architecture: the result is being calculated as the parsing progresses.

Only the basic math operations are supported:

- Addition: `+`
- Subtraction: `-`
- Multiplication: `*`
- Division: `/`

Operations are grouped according to their natural math order but can be prioritized by brackets, like so:

```
(10 + 20) * 3
```

If you want to play with the grammar, you can modify the `.atg` or a `.frame` file.
Then run the following command to produce the updated scanner and parser files:

```
> turbo-coco Calc.atg
```

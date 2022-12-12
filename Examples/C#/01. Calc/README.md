# Calc
Calc is the simplest Turbo Coco/R sample for C#.

When Calc app runs, it asks a user to enter an expression and then calculates the result.
Only the basic math operations are supported:

- Addition `+`
- Subtraction `-`
- Multiplication `*`
- Division `/`

Operations are grouped according to their natural math order but can be prioritized by brackets, like so:

```
(10 + 20) * 3
```

If you want to play with a grammar then you can modify the `.atg` or a `.frame` file.
To produce updated scanner and parser files the following command should be run manually:

```
> turbo-coco Calc.atg
```

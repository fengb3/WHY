# Global Options

Global options are flags/values available to **every** command, parsed before dispatch. Define them
on a single class marked with `[GlobalOptions]`.

## Define a global options class

```csharp
using SuperCli.Attributes;

[GlobalOptions]
public class MyGlobalOptions
{
    [Option("--verbose", ShortName = "-v")]
    public bool Verbose { get; set; }

    [Option("--output-format", ShortName = "-of")]
    public string OutputFormat { get; set; } = "text";
}
```

```bash
app --verbose calc add --x 1 --y 2
app -v -of json tool greet Alice
```

> One `[GlobalOptions]` class per project. It is registered as a **singleton** and can appear
> anywhere on the command line (the parser strips recognized tokens before dispatch).

## Attributes

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class GlobalOptionsAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public sealed class OptionAttribute(string name) : Attribute
{
    public string Name { get; }       // long form, e.g. "--verbose"
    public string? ShortName { get; set; } // optional, e.g. "-v"
}
```

- `Name` is the long form (`--kebab`). Always include the `--` prefix.
- `ShortName` is optional (e.g. `-v`). Omit it for long-form-only options.
- A `bool` property is a **flag** (presence sets it to `true`).

## Default values & environment variables

The property initializer runs, then recognized tokens override it. This makes environment-variable
defaults natural:

```csharp
[Option("--verbose", ShortName = "-v")]
public bool Verbose { get; set; } =
    Environment.GetEnvironmentVariable("VERBOSE") == "true";
```

The generated parser re-runs the initializer each invocation, so a singleton options object never
leaks a previous run's value.

## Inject global options into commands

The options object is a singleton resolved from DI. Inject it via constructor:

```csharp
[Command("calc")]
internal partial class CalcCommand
{
    private readonly MyGlobalOptions _opts;
    public CalcCommand(MyGlobalOptions opts) => _opts = opts;

    [Command("add")]
    public void Add(int x, int y)
    {
        if (_opts.Verbose)
            Console.WriteLine($"[verbose] add x={x}, y={y}");
    }
}
```

## Parse order

Inside `RunAsync`, the framework applies options in this order before dispatch:

1. `--output` is stripped (framework-intrinsic — see [output-formatting.md](output-formatting.md))
2. Global options are stripped and applied to the singleton
3. The remainder is dispatched to the command

So `--verbose` and `--output json` can be freely intermixed with the command path and its options.

## Built-in global option: `--output`

`--output <json|markdown>` is **always** available, even with no `[GlobalOptions]` class — it is
framework-intrinsic, not a user-defined option. See [output-formatting.md](output-formatting.md).

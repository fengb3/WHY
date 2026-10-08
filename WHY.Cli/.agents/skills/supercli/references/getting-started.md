# Getting Started

SuperCli is a .NET 10 source-generator-based CLI framework. All command routing and argument
parsing is generated **at compile time — zero runtime reflection**, which also makes it fully
Native AOT compatible.

## 1. Install

```bash
dotnet add package SuperCli
```

The NuGet package ships both the runtime library and the source generator — no extra setup.
Requires .NET 10+.

## 2. Define a command

Mark a class with `[Command]` to create a command group, and methods with `[Command]` to define
sub-commands. Command classes must be `partial` (the generator extends them):

```csharp
using SuperCli.Attributes;

[Command("calc")]
internal partial class CalcCommand
{
    [Command("add")]
    public void Add(int x, int y)
    {
        Console.WriteLine($"{x} + {y} = {x + y}");
    }
}
```

Every method parameter becomes a CLI option in `--kebab-case`, so `int x` → `--x` and
`outputDir` → `--output-dir`. See [arguments.md](arguments.md).

## 3. Bootstrap and run

```csharp
using SuperCli;

var builder = CliApplication.CreateBuilder();
var app = builder.Build();
return await app.RunAsync(args);
```

`CliApplication.CreateBuilder()` is generated per-project — it registers every `[Command]` class,
filter, `[GlobalOptions]`, and `IOutputFormatter` into the DI container. `RunAsync(args)` returns
`Task<int>` (the process exit code), so `return await app.RunAsync(args);` from top-level statements
sets the exit code.

## 4. Execute

```bash
dotnet run -- calc add --x 1 --y 2
# Output: 1 + 2 = 3
```

## Async & exit codes

Command methods may return:

| Return type            | Meaning                                              |
| ---------------------- | ---------------------------------------------------- |
| `void`                 | Runs synchronously, exit code `0`                    |
| `Task`                 | Async, exit code `0`                                 |
| `int` / `Task<int>`    | Return value becomes the process exit code           |

```csharp
[Command("equal")]
public async Task<int> Equal(int x, int y)
{
    await Task.Delay(50);
    return x == y ? 0 : 1;
}
```

You can also short-circuit from anywhere with `throw new CommandExitException(exitCode)` — see
[arguments.md](arguments.md#exit-codes-and-errors).

## What to read next

- [commands.md](commands.md) — nested commands & command paths
- [arguments.md](arguments.md) — options, positional args, defaults
- [output-formatting.md](output-formatting.md) — JSON / Markdown output
- [aot.md](aot.md) — Native AOT publishing

# Arguments & Options

Method parameters are how you declare a command's CLI inputs. SuperCli turns them into options or
positional arguments at compile time.

## Options (default) — `--kebab-case`

Any parameter **without** `[Argument]` becomes a `--kebab-case` option:

```csharp
[Command("add")]
public void Add(int x, int y) { /* ... */ }
```

```bash
app calc add --x 1 --y 2
```

PascalCase / camelCase parameters are kebab-cased: `outputDir` → `--output-dir`.

## Positional arguments — `[Argument]`

Mark a parameter with `[Argument]` to make it positional — passable by position (no `--name`):

```csharp
[Command("echo")]
public void Echo([Argument] string message, int count = 1) { /* ... */ }

[Command("count")]
public void Count([Argument] string text, [Argument] string format = "short") { /* ... */ }
```

```bash
app calc echo hello --count 3
app tool count "hello world" full
```

`[Argument]` optionally takes a `Description` that overrides XML doc help text.

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ArgumentAttribute : Attribute
{
    public string? Description { get; set; }
}
```

## Default values

Parameters with defaults are optional. Both options and positional arguments support them:

```csharp
public void Greet([Argument] string name, int delay = 200) { /* ... */ }
public void Go(string go = "go", CancellationToken ct = default) { /* ... */ }
```

## Supported types

Parsed from string via the standard converters: `int`, `long`, `double`, `bool`, `string`, enums,
`DateTime`, `Guid`, and nullable variants. `bool` parameters become **flags** (presence = `true`).

## Excluded parameters (auto-injected, not CLI options)

These parameters are **never** CLI options — the framework resolves them for you:

| Parameter type                | Resolved as                                              |
| ----------------------------- | -------------------------------------------------------- |
| `CancellationToken`           | A token cancelled on `Ctrl+C`                            |
| `CommandExecuteContext`       | The current execution context (see [filters.md](filters.md)) |
| `IOutputFormatter`            | The formatter singleton (see [output-formatting.md](output-formatting.md)) |
| Any other DI-registered type  | Resolved from the service provider (see [di.md](di.md))  |

```csharp
// ct is NOT --ct; it's the Ctrl+C token. go is the only real option here.
[Command("go-your")]
public void Go(string go = "go", CancellationToken ct = default) { /* ... */ }
```

## Exit codes & errors

Return `int` or `Task<int>` to control the exit code:

```csharp
[Command("equal")]
public async Task<int> Equal(int x, int y) => x == y ? 0 : 1;
```

To short-circuit from a filter or anywhere in the pipeline, throw:

```csharp
// Sets the exit code and stops the pipeline. Optional message goes to stderr.
throw new CommandExitException(2, "something went wrong");

// Bad CLI input — exit code 1, message to stderr. Used by the built-in parsers.
throw new ArgumentParseException("Option '--output' requires a value.");
```

The runtime maps these to exit codes:

| Exception                  | Exit code | Output                              |
| -------------------------- | --------- | ----------------------------------- |
| `CommandExitException`     | `ex.ExitCode` | `ex.Message` to stderr (if any) |
| `CommandNotFoundException` | `1`       | message + help text                 |
| `ArgumentParseException`   | `1`       | message to stderr                   |

> Note: `[Option]` is **not** used on command parameters — it is only for `[GlobalOptions]`
> properties. See [global-options.md](global-options.md).

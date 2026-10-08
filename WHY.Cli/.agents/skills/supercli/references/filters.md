# Filters (Middleware)

Filters implement `ICommandFilter` and wrap command execution like middleware — run code before
and/or after the command, or short-circuit entirely.

## The interface

```csharp
public interface ICommandFilter
{
    Task HandleAsync(CommandExecuteContext context, Func<CommandExecuteContext, Task> next);
}
```

Call `await next(context)` to continue down the pipeline; omit it to short-circuit.

```csharp
public class LoggingFilter : ICommandFilter
{
    public async Task HandleAsync(CommandExecuteContext context, Func<CommandExecuteContext, Task> next)
    {
        Console.WriteLine($"Before: {context.CommandName}");
        await next(context);
        Console.WriteLine($"After: {context.CommandName}");
    }
}
```

## Attach filters with `[CommandFilter<...>]`

```csharp
[Command("my")]
[CommandFilter<LoggingFilter>]                 // class-level: all methods in the class
internal partial class MyCommand
{
    [Command("action")]
    [CommandFilter<ValidationFilter, HappyFilter>]   // method-level: this method only
    public void Action(string name) { /* ... */ }
}
```

The generic attribute accepts **1–16 filter types** in execution order:

```csharp
[CommandFilter<A, B, C>] // runs A, then B, then C
```

- **Class-level** filters wrap every method in the class.
- **Method-level** filters wrap just that method.
- Filters are plain classes registered as **scoped** DI services — they can themselves take
  constructor dependencies.

## Execution order

```
Class-level filters  →  Method-level filters  →  ParseArgsFilter  →  Command method
   (outermost)                                                  (innermost)
```

- **Outermost** = class filters. They see the raw arguments and can reject the call early.
- **ParseArgsFilter** is the built-in innermost filter: it parses the `--kebab`/positional tokens
  into the strongly-typed arguments object and binds them to the method.
- The command method runs last.

A filter that doesn't call `next` prevents everything after it (including the command) from running —
useful for validation:

```csharp
public class ValidationFilter : ICommandFilter
{
    public async Task HandleAsync(CommandExecuteContext context, Func<CommandExecuteContext, Task> next)
    {
        if (context.Arguments.Length == 0)
        {
            Console.WriteLine("No arguments provided. Aborted.");
            return; // short-circuit: command never runs
        }
        await next(context);
    }
}
```

## CommandExecuteContext

The context carries everything a filter or command needs about the current invocation:

| Property          | Description                                              |
| ----------------- | -------------------------------------------------------- |
| `CommandName`     | The routed command path (e.g. `"calc add"`)              |
| `Arguments`       | Raw CLI arguments (post global-option strip)             |
| `CommandDepth`    | How deep in the command tree (root = 0)                  |
| `CancellationToken` | Cancelled on `Ctrl+C`                                  |
| `ParsedArgs`      | The strongly-typed parsed arguments object               |
| `OutputFormat`    | Active `--output` value (`"json"` / `"markdown"`)        |
| `Items`           | `Dictionary<string, object>` for passing data between filters |
| `ExitCode`        | Exit code slot                                           |

> `CommandExecuteContext` is a **singleton** mutated per-invocation. That's why it's safe to inject
> into filters and commands — by the time your code runs, the framework has set its current values.

Filters and commands can stash per-request data in `context.Items` to hand off to a later stage.

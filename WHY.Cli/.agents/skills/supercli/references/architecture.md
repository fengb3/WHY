# Under the Hood

A quick look at how SuperCli works internally — useful when you want to understand the
reflection-free guarantee, or find the generated code while debugging.

## Everything is generated at compile time

When you build, the SuperCli source generator scans your code for `[Command]`, `[CommandFilter]`,
`[GlobalOptions]`, and `[Output]`, and emits ordinary C# that does all the work:

- **Routing** — a prefix tree compiled into nested `switch/case`. No attribute scan at startup.
- **Argument parsing** — a strongly-typed args class and parser per command.
- **Filters** — the middleware chain is wired up per command.
- **Serialization** — per-type JSON/Markdown writers (only when you use `[Output]`).

Because every type and route is known at compile time, the runtime does **zero reflection** — which
is also why Native AOT works out of the box (see [aot.md](aot.md)).

## Generated files

Everything the generator emits is plain `.g.cs` you can read. After a build you'll find them under
`obj/<Config>/<tfm>/generated/SuperCli.SourceGenerator/SuperCli.SourceGenerator.CommandGenerator/`
in your project:

| File | What it contains |
| --- | --- |
| `CliApplication.g.cs` | `CreateBuilder()` (DI registration) and `RunAsync()` (option parsing, dispatch, Ctrl+C, exit codes) |
| `CommandDispatch.g.cs` | The routing prefix tree → nested `switch` |
| `CommandExtensions.g.cs` | Per-command arg binding + method invocation |
| `CommandArgs.g.cs` | Per-command args records + the `ParseArgsFilter` |
| `CommandFiltersPipelines.g.cs` | The filter chain per command |
| `CommandHelp.g.cs` | `--help` / `--version` output |
| `GlobalOptions.g.cs` | Global-option parser (only if you have a `[GlobalOptions]` class) |
| `OutputFormatter.g.cs`, `OutputOptions.g.cs` | `IOutputFormatter` impl + `--output` parser (only if you use `[Output]`) |

Reading `CliApplication.g.cs` is the fastest way to see exactly how your app boots and how exit
codes are mapped. Reading `CommandArgs.g.cs` shows how a specific command's options are parsed.

## Build tip

If the generator DLL gets locked by `CSharpLanguageServer` during a build (the IDE holding it
open), kill that process and retry the build.

## Filter pipeline order (recap)

```
Class-level filters  →  Method-level filters  →  ParseArgsFilter  →  Command method
```

`ParseArgsFilter` parses CLI tokens into the command's args object; it is always the innermost
filter, just before your method runs. See [filters.md](filters.md).

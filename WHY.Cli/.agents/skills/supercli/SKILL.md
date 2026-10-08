---
name: supercli
description: Guide to the SuperCli .NET 10 source-generator CLI framework (zero runtime reflection, Native AOT compatible). Covers defining commands with [Command], --kebab options and [Argument] positional args, [GlobalOptions], ICommandFilter middleware, [Output]/IOutputFormatter JSON+Markdown output, dependency injection, --help/--version, and Native AOT publishing. Use when building or debugging a SuperCli CLI app, or when writing commands, filters, global options, or [Output] DTOs in a project that references the SuperCli package.
---

# SuperCli

A .NET 10 source-generator-based CLI framework. All command routing and argument parsing is
generated **at compile time — zero runtime reflection**, and fully Native AOT compatible.

This skill is a usage guide for **consumers** of the SuperCli NuGet package. Each topic lives in its
own file — read the ones relevant to your task.

## Topics

| Topic | File | When to read |
| --- | --- | --- |
| **Getting started** | [getting-started.md](references/getting-started.md) | Install, minimal app, build & run, async/exit codes |
| **Commands & routing** | [commands.md](references/commands.md) | `[Command]` on class/method, nested commands & paths, `partial` |
| **Arguments & options** | [arguments.md](references/arguments.md) | `--kebab` options, `[Argument]` positional, defaults, excluded params |
| **Global options** | [global-options.md](references/global-options.md) | `[GlobalOptions]` + `[Option]`, env-var defaults, parse order |
| **Filters (middleware)** | [filters.md](references/filters.md) | `ICommandFilter`, `[CommandFilter<...>]`, pipeline order, context |
| **Output formatting** | [output-formatting.md](references/output-formatting.md) | `[Output]`, `IOutputFormatter`, `--output json\|markdown` |
| **Help & version** | [help-version.md](references/help-version.md) | `--help`/`-h`/`-?`, `--version`, XML doc → help, `[CliDescription]` |
| **Dependency injection** | [di.md](references/di.md) | `builder.Services`, constructor & method-param injection, lifetimes |
| **Native AOT** | [aot.md](references/aot.md) | `PublishAot`, reflection-free design, publishing native binaries |
| **Under the hood** | [architecture.md](references/architecture.md) | What gets generated, where to find `.g.cs` files, build tips |

## Quick reference — "how do I…"

- **Add a command** → mark a `partial` class and/or method with `[Command("name")]` ([commands.md](references/commands.md))
- **Take a `--flag` / `--value`** → add a method parameter; `outputDir` becomes `--output-dir` ([arguments.md](references/arguments.md))
- **Take a positional arg** → mark the parameter `[Argument]` ([arguments.md](references/arguments.md#positional-arguments--argument))
- **Add a global flag** → `[GlobalOptions]` class + `[Option]` properties ([global-options.md](references/global-options.md))
- **Run code before/after a command** → `ICommandFilter` + `[CommandFilter<T>]` ([filters.md](references/filters.md))
- **Output JSON or Markdown** → `[Output]` DTO + inject `IOutputFormatter`, control via `--output` ([output-formatting.md](references/output-formatting.md))
- **Set exit code** → return `int`/`Task<int>`, or throw `CommandExitException` ([arguments.md](references/arguments.md#exit-codes--errors))
- **Customize root help text** → `[assembly: CliDescription("…")]` ([help-version.md](references/help-version.md))
- **Inject a service** → register on `builder.Services`, take it as a ctor/method param ([di.md](references/di.md))
- **Publish a native binary** → `<PublishAot>true</PublishAot>` then `dotnet publish -r <rid>` ([aot.md](references/aot.md))

## Core facts (always true)

- **Zero runtime reflection.** Routing, parsing, and serialization are all source-generated.
- **`RunAsync(args)`** (not `Run`) returns `Task<int>` — the exit code.
- **Command classes must be `partial`.**
- **`--output json|markdown`** is framework-intrinsic (works with no `[GlobalOptions]`); default `json`.
- **Filter order:** class filters → method filters → `ParseArgsFilter` → command method.
- **Native AOT** works out of the box; `IL2026`/`IL3050` warnings = a bug to fix, not suppress.

## Bootstrap (the shape of every SuperCli app)

```csharp
using SuperCli;

var builder = CliApplication.CreateBuilder();
var app = builder.Build();
return await app.RunAsync(args);
```

Curious how it works, or debugging generated `.g.cs` output? See [architecture.md](references/architecture.md).

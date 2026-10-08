# Dependency Injection

SuperCli is built on `Microsoft.Extensions.DependencyInjection`. `CliApplication.CreateBuilder()`
returns a `CliApplicationBuilder` whose `Services` is a real `IServiceCollection` — register
anything you need.

## The builder

```csharp
public class CliApplicationBuilder
{
    public IServiceCollection Services { get; } = new ServiceCollection();
}
```

`CreateBuilder()` (generated per-project) pre-registers:

| Registration                         | Lifetime   | Why                                   |
| ------------------------------------ | ---------- | ------------------------------------- |
| Every `[Command]` class              | Scoped     | One instance per invocation (per scope) |
| Every `[CommandFilter]` filter class | Scoped     | Resolved per invocation               |
| The `[GlobalOptions]` class          | Singleton  | Parsed once, shared                   |
| `CommandExecuteContext`              | Singleton  | Mutated per-invocation state carrier  |
| `IOutputFormatter` → `OutputFormatterImpl` | Singleton | Reads active format at call time |

## Add your own services

```csharp
var builder = CliApplication.CreateBuilder();
builder.Services.AddSingleton<IHttpClient, MyHttpClient>();
builder.Services.AddScoped<IDbContext, SqlDbContext>();
var app = builder.Build();
```

## Inject into commands & filters

Anything registered in DI can be injected via **constructor** or **method parameter**:

```csharp
// Constructor injection (commands & filters)
[Command("server")]
internal partial class ServerCommand
{
    private readonly IOutputFormatter _fmt;
    private readonly IDbContext _db;
    public ServerCommand(IOutputFormatter fmt, IDbContext db) { /* ... */ }
}

// Method-parameter injection (commands)
[Command("port")]
public void Port(IOutputFormatter formatter) { /* ... */ }
```

### Auto-excluded parameter types

The generator **excludes** these parameter types from CLI option generation and resolves them via
DI instead (so they never become `--kebab` options):

- `CancellationToken` → the `Ctrl+C` token
- `CommandExecuteContext` → the current context
- `IOutputFormatter` → the formatter singleton
- Any other type registered in the DI container

This is how `public void Port(IOutputFormatter formatter)` works without `formatter` becoming a
`--formatter` option.

## Lifetime notes

- Commands and filters are **scoped** — a fresh instance is created for each command invocation
  within the dispatch scope. Constructor state won't leak across runs.
- `CommandExecuteContext` and the `[GlobalOptions]` object are **singletons** — safe to capture in a
  long-lived service, and their per-invocation values are set by the framework before your code runs.
- Prefer constructor injection for dependencies used by many methods; use method-parameter injection
  for one-off dependencies like `IOutputFormatter`.

## Related: EasyDependencyInjection

The solution also includes a separate DI source generator (`src/EasyDependencyInjection*`) with
`[ServiceRegister]` and `[FromServices]` attributes. It is an independent companion project, not
required to use SuperCli's DI.

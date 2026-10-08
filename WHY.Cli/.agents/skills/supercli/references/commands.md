# Commands & Routing

Commands are defined with `[Command("name")]` on a **class** (a command group) and/or on
**methods** (leaf commands). The source generator builds a prefix tree at compile time and emits
nested `switch/case` routing — no runtime reflection, no attribute scan at startup.

## Attribute

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CommandAttribute(string Name) : Attribute;
```

`Name` is a single path segment (no spaces). It becomes one node in the route.

## A simple command group

```csharp
[Command("calc")]
internal partial class CalcCommand
{
    [Command("add")] public void Add(int x, int y) { /* ... */ }
    [Command("sub")] public void Subtract(int x, int y) { /* ... */ }
}
```

```bash
app calc add --x 1 --y 2
app calc sub --x 5 --y 3
```

## Nested commands (multi-level paths)

A nested class that also has `[Command]` extends the path. The framework walks the containing-type
chain to build `FullClassCommandPath`:

```csharp
[Command("calc")]
internal partial class CalcCommand
{
    [Command("advanced")]
    internal partial class AdvancedCalcCommand
    {
        [Command("pow")]
        public void Power(int x, int y) { /* ... */ }
    }
}
```

```bash
app calc advanced pow --x 2 --y 10
# 2 ^ 10 = 1024
```

The route is the concatenation of every `[Command]` name from the outermost class to the method:
`calc` → `advanced` → `pow`.

## Requirements & conventions

- **`partial` is required** on every `[Command]` class — the generator adds the invocation plumbing
  to it. (The build will fail with a clear message if you forget it.)
- **Visibility:** classes can be `internal` or `public`. The generator handles both.
- **Non-command members are fine:** private helpers, fields, and constructors live alongside
  command methods untouched (e.g. `private int Helper(int n)` is ignored by routing).
- **Naming:** command names are matched exactly as written. Use lowercase kebab-style manually
  (e.g. `[Command("do-stuff")]`) — the framework does not auto-kebab command names, only options.

## How routing works (briefly)

`CommandDispatchEmitter` builds a prefix tree from all command paths and emits a `CommandDispatcher`
with nested `switch` statements keyed on each token. A path that matches no command throws
`CommandNotFoundException` (exit code `1`, with help text). See [architecture.md](architecture.md).

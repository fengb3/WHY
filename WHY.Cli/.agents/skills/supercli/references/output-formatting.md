# Output Formatting (`--output`)

SuperCli generates reflection-free serializers so commands can emit **JSON** or **Markdown** without
any runtime reflection — fully Native AOT compatible.

## The global `--output` option

`--output <format>` is a **framework-intrinsic** global option — always available, even with no
`[GlobalOptions]` class. Long form only (no short name).

```bash
app server list                       # default: json
app server list --output json
app server list --output markdown
app --output markdown server list     # can appear anywhere
app --output yaml server list         # error: Unknown output format 'yaml'. Known: json, markdown.
```

- Default format: **`json`**.
- Unknown or missing value → `ArgumentParseException`, exit code `1`.
- The token is stripped before dispatch, so it never leaks into command argument parsing.

## Mark output types with `[Output]`

Put `[Output]` on the class/struct you want serialized. The generator collects every `[Output]`
type and its closure (nested `[Output]` properties, `List<T>`, `T[]`) and emits a writer per type.

```csharp
using SuperCli.Attributes;

[Output]
public class PortInfo
{
    public int Number { get; set; }
    public string Protocol { get; set; } = "";
}

[Output]
public class ServerInfo
{
    public string Name { get; set; } = "";
    public PortInfo PrimaryPort { get; set; } = new();          // nested [Output]
    public List<PortInfo> Ports { get; set; } = new();          // [Output] collection
}
```

> Nested reference-type properties **must** also be marked `[Output]`. The generator does not
> reflect over arbitrary unmarked types (that would break AOT trimming).

## Serialize with `IOutputFormatter`

```csharp
public interface IOutputFormatter
{
    string Format { get; }            // active format from CommandExecuteContext
    string Serialize<T>(T value);     // returns formatted string (json | markdown)
}
```

Inject it (constructor or method parameter) and call `Serialize`:

```csharp
[Command("server")]
internal partial class ServerCommand
{
    private readonly IOutputFormatter _fmt;
    public ServerCommand(IOutputFormatter fmt) => _fmt = fmt;

    [Command("list")]
    public void List()
    {
        var servers = new List<ServerInfo> { /* ... */ };
        Console.WriteLine(_fmt.Serialize(servers));   // picks json or markdown from --output
    }
}
```

`IOutputFormatter` is a **singleton** that reads the active format from `CommandExecuteContext` at
call time, so the same instance serves every command.

## Constructor vs method-parameter injection

Both work — `IOutputFormatter` is in the auto-excluded set (see [di.md](di.md)):

```csharp
// Constructor injection (preferred for commands that use it often)
public ServerCommand(IOutputFormatter fmt) { ... }

// Method-parameter injection (good for one-off use)
[Command("port")]
public void Port(IOutputFormatter formatter)
{
    Console.WriteLine(formatter.Serialize(new PortInfo { Number = 8080, Protocol = "http" }));
}
```

## Supported shapes

The generated writers handle, per `[Output]` type:

- **Scalars** — `string`, `bool`, all numeric types, enums, `DateTime`, `Guid`, `char`, etc.
- **Nullable value types** — `int?` serializes as a number, or `null` (JSON) / `(none)` (Markdown).
- **Scalar collections** — `List<string>`, `int[]` (elements stay their JSON type, not quoted strings).
- **Enum collections** — `List<ServiceStatus>`.
- **Nested `[Output]`** — a property whose type is itself `[Output]`.
- **`[Output]` collections** — `List<ServerInfo>`, `PortInfo[]`.

### Output shape notes

- **JSON:** indented, `camelCase` property names, arrays for collections, proper `null` handling.
- **Markdown:**
  - A collection whose element type has **only scalar** properties renders as a **table**
    (`| Col | Col |` / `| --- | --- |`).
  - A collection with **nested or non-scalar** properties renders per-item blocks (so nested data
    is never silently dropped).
  - A single object renders as a `## TypeName` definition block with `- **Prop**: value` lines.

## Why not `System.Text.Json` source generation?

One source generator cannot consume another generator's output, so STJ's `JsonSerializerContext`
could never see a generated context. Instead the JSON writers are emitted directly (via
`Utf8JsonWriter`), mirroring the Markdown renderer. This keeps serialization fully self-contained
and trim/AOT-safe. See [aot.md](aot.md).

# Native AOT

SuperCli is designed to publish as **Native AOT** — a single self-contained native binary with fast
startup and low memory, with no runtime reflection.

## Why it works

Everything is resolved at compile time by the source generator:

- **Routing** — a prefix tree compiled into nested `switch/case` (no attribute scan at startup).
- **Argument parsing** — strongly-typed per-command args classes generated up front.
- **Serialization** — per-type JSON/Markdown writers emitted directly, dispatched through a closed
  `switch` over the discovered `[Output]` types.

Because every type is known statically, the trimmer never needs to preserve reflection metadata.
There are no `[RequiresUnreferencedCode]`, `[DynamicDependency]`, or `MakeGenericType` calls.

## Publish an AOT binary

Enable it in your project:

```xml
<!-- YourApp.csproj -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

From your project directory, publish for a RID:

```bash
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64
```

Then run the native binary directly:

```bash
# Windows
bin/Release/net10.0/win-x64/publish/YourApp.exe server list --output markdown
# Linux
./bin/Release/net10.0/linux-x64/publish/YourApp server list --output markdown
```

> You still develop and `dotnet run` exactly as usual — `PublishAot` only changes what
> `dotnet publish` produces.

## The AOT gate

The authoritative check: the publish must succeed with **zero** `IL2026` / `IL3050` trim/AOT
warnings. Any such warning means reflection crept in somewhere and is a failure to fix before
shipping — not something to suppress with `<TrimmerRootDescriptor>` or `#pragma`.

## Serialization is AOT-safe by construction

A generic `Serialize<T>(T value)` is AOT-safe **iff no reflection runs over `T`**. The generated
implementation routes every call through a closed switch — one concrete arm per registered type:

```csharp
// simplified generated code
return value switch
{
    ServerInfo v       => OutputJson.WriteServerInfo(writer, v),
    List<ServerInfo> v => OutputJson.WriteListServerInfo(writer, v),
    // …one arm per [Output] type and its collections…
    _ => throw new NotSupportedException($"Type {typeof(T)} is not marked [Output].")
};
```

Each arm is concrete and trimmer-safe. You just call `formatter.Serialize(myObj)` and the right
writer is selected at compile time.

## Why not `System.Text.Json` source generation?

`System.Text.Json` source generation (`JsonSerializerContext`) is itself a source generator. **One
source generator cannot see another generator's output** (Roslyn runs them independently), so STJ
would never observe a context that SuperCli emits. The fix is to emit the JSON writers directly via
`Utf8JsonWriter`, mirroring the Markdown renderer — fully self-contained, no STJ coupling, no
reflection.

## AOT-hostile things to avoid in your commands

- `JsonSerializer.Serialize(object, ...)` overloads (IL2026) — use the generated `IOutputFormatter`.
- `typeof(T).GetProperties()` / `Activator.CreateInstance` / `MakeGenericType`.
- Reflection-based serializers (YamlDotNet, Newtonsoft with reflection, etc.).
- Marking only an outer type `[Output]` while leaving a nested reference type unmarked — the
  generator emits diagnostic `SCOUT001` telling you to mark it.

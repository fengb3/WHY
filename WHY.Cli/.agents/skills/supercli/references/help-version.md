# Help & Version

SuperCli generates contextual help and version output automatically. You mostly just write XML doc
comments.

## Built-in flags

| Flag                 | Scope        | Description                                  |
| -------------------- | ------------ | -------------------------------------------- |
| `--help` / `-h` / `-?` | Every level | Show contextual help for that node           |
| `--version`          | Root only    | Print the application version                |

```bash
app --help              # all top-level commands + global options
app calc --help         # commands under "calc"
app calc add --help     # help for "calc add" (its options, args, description)
app --version           # the version
```

`--help` works at the root, at any branch (command group), and at any leaf (command method). The
output adapts to that node — listing child commands, or the command's own options/arguments.

## XML documentation → help text

The `<summary>`, `<param>`, and `<example>` tags on command classes and methods become help text
automatically — no extra attributes needed:

```csharp
/// <summary>
/// Adds two integers together and prints the result.
/// </summary>
/// <param name="x">The first operand.</param>
/// <param name="y">The second operand.</param>
/// <example>calc add --x 1 --y 2</example>
[Command("add")]
public void Add(int x, int y) { /* ... */ }
```

This shows up in `app calc add --help`. Keep XML docs on your commands and they double as CLI help.

> For this to work, your project must **emit XML documentation** (most templates do; otherwise add
> `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to the `.csproj`).

## Application description — `[CliDescription]`

Set the description shown at the top of root help with an assembly-level attribute:

```csharp
using SuperCli;

[assembly: CliDescription("MyApp — a tool that does useful things")]
```

```csharp
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class CliDescriptionAttribute : Attribute
{
    public string Description { get; }
    public CliDescriptionAttribute(string description);
}
```

## Global options in help

Properties on your `[GlobalOptions]` class appear in the Options section of help at every level
(their XML doc / `[Option]` becomes the description). The framework's intrinsic `--output` option is
also listed (e.g. `--output  Output format: json or markdown`). See
[global-options.md](global-options.md).

## Version source

`--version` outputs the value from the `$(Version)` MSBuild property at build time, falling back to
the assembly version at runtime if unset. Set it in your `.csproj` or `Directory.Build.props`:

```xml
<PropertyGroup>
  <Version>1.2.3</Version>
</PropertyGroup>
```

# Vectra Compiler - Claude Code Guide

## Project Overview

Vectra is a statically-typed programming language implemented in C# (.NET 10). This repository is the multi-phase compiler that transforms Vectra source files (`.vec`) into bytecode.

## Build & Test Commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests (CI mode)
dotnet test --no-build --configuration Release

# Run the compiler
dotnet run --project VectraCompiler build path/to/project.vpkg
```

## Solution Structure

The solution uses the `.slnx` format (modern .NET). Projects are organized by compiler phase:

| Project | Role |
|---|---|
| `VectraCompiler` | CLI entry point (Spectre.Console.Cli) |
| `VectraCompiler.Core` | Shared infrastructure: logging, `DiagnosticBag`, extensions |
| `VectraCompiler.Package` | Reads `.vpkg` / `.vmod` manifests, resolves dependencies |
| `VectraCompiler.AST` | Lexer → tokens, Parser → AST |
| `VectraCompiler.Bind` | Semantic analysis, symbol binding, type resolution |
| `VectraCompiler.Analysis` | Static checks: uninitialized vars, unreachable code |
| `VectraCompiler.Lower` | AST lowering to intermediate representation |
| `VectraCompiler.Emit` | Code generation → bytecode (VBC/VDL/VDI/VDS) |
| `VectraCompiler.Analysis.Tests` | NUnit tests for the analysis phase |
| `TestCode` | Sample `.vpkg`/`.vmod`/`.vec` files for manual testing |

### Compiler Pipeline Order

```
Package Resolution → Parse → Bind → Analyze → Lower → Emit
```

## Architecture Conventions

### Error Handling
- Use `Result<T>` with `.Ok` property for phase outputs.
- Collect diagnostics in `DiagnosticBag` rather than throwing exceptions.

### Bound IR
- `BoundStatement` and `BoundExpression` carry a `TypeSymbol` and `SourceSpan`.
- `BoundContext` holds scope, member symbols, expected type, and diagnostics.
- Entry points: `BindMethodBody()`, `BindConstructorBody()`.
- Dispatchers: `BindStatement()`, `BindExpression()`.

### Symbol Hierarchy (`VectraCompiler.Bind/Models/Symbols/`)
- `Symbol` → `TypeSymbol` → `NamedTypeSymbol`, `BuiltInTypeSymbol`
- `CallableSymbol`: methods and constructors
- `VariableSymbol`: local variables and fields

### CLI Commands
- Each command is a class inheriting `AsyncCommand<TSettings>`.
- Settings classes define arguments and options.
- Add new commands in `VectraCompiler/Commands/`.

### Logging
- Use the centralized `Logger` from `VectraCompiler.Core.Logging`.
- Sinks: `FileSink`, `SpectreConsoleSink`.

## C# Conventions

- **Nullable**: enabled — annotate all reference types, no `!` suppressions without justification.
- **Implicit usings**: enabled — do not add redundant `using` statements for common .NET namespaces.
- **Target framework**: `net10.0` for all projects.
- **Namespaces**: match project name, e.g. `VectraCompiler.Bind.Models.Symbols`.
- **File layout**: group by type — `Models/`, `Interfaces/`, `Expressions/`, `Statements/`.

## Testing

- Framework: NUnit 4.x
- Test class suffix: `*Tests`
- Use `AnalyzerTestBase` and `BoundTreeBuilder` helpers where applicable.
- New tests go in `VectraCompiler.Analysis.Tests` (or a new `*.Tests` project for the relevant phase).

## Vectra Language Reference (quick)

### File Types
- `.vpkg` — package manifest (lists modules)
- `.vmod` — module manifest (type, dependencies, source paths)
- `.vec` — Vectra source code

### Keywords
`space`, `class`, `let`, `new`, `this`, `return`, `get`, `set`, `true`, `false`, `number`, `string`, `bool`, `void`

### Temporary Built-ins (will be removed)
`Print(string)`, `PrintLine(string)`, `Read()`, `ReadLine()`, `ReadInt()`

### Output Formats
- `VBC` — Vectra ByteCode (executable)
- `VDL` — Vectra Dynamic Library
- `VDI` — Vectra Definition Interface
- `VDS` — Vectra Debug Symbols

## CI / Release

- GitHub Actions: `.github/workflows/ci.yml` (build + test), `release.yml` (semantic-release).
- Versioning: semantic-release driven by conventional commits.
- Current version: `0.0.0-dev`.

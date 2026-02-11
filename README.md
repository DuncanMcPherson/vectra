# Vectra Compiler

Vectra is a modern, statically-typed programming language. This repository contains the source code for the Vectra Compiler, built on .NET 10.

## Project Overview

The Vectra Compiler is a multi-stage compiler designed to process Vectra source files (`.vec`) organized into modules (`.vmod`) and packages (`.vpkg`). It features a modular architecture with a dedicated CLI and specialized libraries for AST generation, package management, and core utilities.

## Project Structure

The solution is divided into several projects:

- **VectraCompiler**: The main CLI application. Handles command-line arguments, build orchestration, and provides a rich terminal UI using Spectre.Console.
- **VectraCompiler.AST**: Contains the Lexer and Parser for the Vectra language. It transforms raw source code into an Abstract Syntax Tree.
- **VectraCompiler.Package**: Responsible for reading and resolving package (`.vpkg`) and module (`.vmod`) metadata.
- **VectraCompiler.Bind**: Handles semantic analysis and symbol binding. It ensures that types, methods, and variables are correctly defined and referenced.
- **VectraCompiler.Core**: Provides common infrastructure such as logging, error handling (`DiagnosticBag`), and various utility extensions.
- **TestCode**: Contains sample Vectra projects, modules, and source files used for testing the compiler.

## The Vectra Language

Vectra is structured around "spaces" (namespaces) and classes.

### Keywords
- **Structure**: `space`, `class`
- **Types**: `number`, `string`, `bool`, `void`
- **Declarations**: `let`, `new`, `this`
- **Control Flow**: `return`, `get`, `set`
- **Literals**: `true`, `false`

#### Temporary builtins
- `Print(string)`
- `Read()`

**Note:** These builtins are temporary and will be removed in a future release. They are intended to enhance the developer experience while the language is in development. Using them in production code is not recommended.

### Operators
- **Arithmetic**: `+`, `-`, `*`, `/`, `%`
- **Comparison**: `==`, `!=`, `<`, `>`, `<=`, `>=`
- **Assignment**: `=`, `+=`, `-=`, `*=`, `/=`
- **Unary**: `!`, `-`

### Key Features
- **Spaces**: Hierarchical namespaces to organize code (e.g., `space MyProject.Utilities;`).
- **Classes**: Support for classes with constructors, fields, properties, and methods.
- **Static Typing**: Explicit and inferred types for variables and members.
- **Properties**: Support for getters and setters.

## File Formats

### `.vpkg` (Package File)
Defines the top-level package and the modules it contains.
```vpkg
package MyPackage
module MyModule ./MyModule.vmod
```

### `.vmod` (Module File)
Defines module metadata, dependencies, references, and source files.
```vmod
module MyModule
metadata {
    type Executable
}
dependencies {
    OtherModule
}
sources {
    ./src/Main.vec
}
```

### `.vec` (Vectra Source File)
Contains the actual Vectra code.
```vec
space Main;

class Program {
    void Main() {
        let message = "Hello, Vectra!";
        // ...
    }
}
```

## Compiled Output
Vectra compiles to 4 different output formats, each with a specific use:
- **VBC**: (Vectra ByteCode) This is the executable format for running a Vectra program
- **VDL**: (Vectra Dynamic Library) A sharable library format that can be imported and referenced from code
- **VDI**: (Vectra Definition Interface) A public definitions format that defines publicly available types and methods contained in a `VDL`
- **VDS**: (Vectra Debug Symbols) A debug symbols format that contains debugging information for a Vectra program or library

## Getting Started

### Prerequisites
- .NET 10.0 SDK
- An IDE that supports `.slnx` files (e.g., JetBrains Rider or Visual Studio 2022)

### Building the Compiler
You can build the solution using the .NET CLI:
```bash
dotnet build
```

### Running the Compiler
To build a Vectra package:
```bash
dotnet run --project VectraCompiler build path/to/project.vpkg
```

Common build options:
- `--log-level`: Set the logging level (Trace, Debug, Info, Warning, Error).
- `--log-dir`: Specify the directory for log files.
- `--no-color`: Disable colored output in the console.

## Development

The compiler follows a standard multi-phase approach:
1. **Metadata Phase**: Resolve packages and modules.
2. **Parse Phase**: Lexical analysis and parsing of source files into AST.
3. **Bind Phase**: (Partially developed) Semantic analysis and symbol binding.
4. **Analyze Phase**: (In development) Further semantic checks and validations.
5. **Lower Phase**: (In development) Transformation of the AST into a lower-level representation.
6. **Emit Phase**: (In development) Code generation.

Logging is handled via `VectraCompiler.Core.Logging.Logger`, which supports multiple sinks including a Spectre.Console sink for interactive terminal output.

## Binder Decisions Log

1. **Bound IR types**

   Even if you start tiny, commit to the distinction:
  - `BoundStatement` / `BoundExpression`
    - Both carry:
      - `TypeSymbol` (for expressions; statements may have `void`/`none`)
      - `SourceSpan` (so diagnostics point to real code)
  - Start with the bound equivalents of what you already parse:
    - `BoundReturnStatement`
    - `BoundExpressionStatement`
    - `BoundVariableDeclarationStatement`
    - `BoundLiteralExpression`
    - `BoundIdentifierExpression` (resolves to symbol)
    - `BoundBinaryExpression`
    - `BoundAssignmentExpression`
    - `BoundCallExpression`
2. **Context object**

   A `BindContext` (record/class) that holds:
- `Scope` (locals)
- `MemberSymbol` (current method/ctor)
- `TypeSymbol? ExpectedType` (optional, but extremely useful)
- diagnostics sink

This is the “environment” your binder needs to not become a spaghetti monster.

3. **Entry points**

   Give yourself two clear doorways:
- `BindMethodBody(MethodSymbol method, BlockStatementNode body, Scope parentScope)`
- `BindConstructorBody(ConstructorSymbol ctor, BlockStatementNode body, Scope parentScope)`
- 
Even if constructors are basically methods-with-void right now, separating them early prevents pain later (field init, base/this calls, definite assignment of this, etc.).

4. **Two dispatchers**
- `BindStatement(StatementNode node, BindContext ctx)`
- `BindExpression(ExpressionNode node, BindContext ctx)`

Everything funnels through those. Internals can be partial classes by category so it stays readable.

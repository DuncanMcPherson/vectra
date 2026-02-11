namespace VectraCompiler.Core.Errors;

public record ErrorInfo(
    ErrorCode Code,
    string Title,
    string Description,
    string? HowToFix = null,
    string? Example = null);

public static class ErrorCatalog
{
    private static readonly IReadOnlyDictionary<ErrorCode, ErrorInfo> Map = new Dictionary<ErrorCode, ErrorInfo>
    {
        [ErrorCode.FileNotFound] = new(
            ErrorCode.FileNotFound,
            "File not found",
            "The specified file could not be found.",
            "Make sure the file exists and that you are specifying the correct path."),
        [ErrorCode.PackageFormatInvalid] = new(
            ErrorCode.PackageFormatInvalid,
            "Invalid package format",
            "The specified package file is not a valid Vectra package.",
            "Check that the package file has the correct extension (.vpkg) and that it is correctly formatted.",
            """
            package Vectra.Core
            
            module Vectra.Core Vectra.Core/Vectra.Core.vmod
            """),
        [ErrorCode.ModuleNotFound] = new(
            ErrorCode.ModuleNotFound,
            "Module not found",
            "The specified module could not be found.",
            "Make sure the module exists and that you are specifying the correct path."),
        [ErrorCode.ModuleFormatInvalid] = new(
            ErrorCode.ModuleFormatInvalid,
            "Invalid module format",
            "The specified module file is not a valid Vectra module.",
            "Check that the module file has the correct extension (.vmod) and that it is correctly formatted.",
            """
            module Vectra.Core
            
            metadata {
                type library
            }
            
            sources {
                src/**/*.vec
            }
            
            references {
                Vectra.CLI
            } 
            """),
        [ErrorCode.CircularDependencyDetected] = new(
            ErrorCode.CircularDependencyDetected,
            "Circular dependency detected",
            "A circular dependency was detected in the module dependencies.",
            "Check that the module dependencies are not circular."
            ),
        [ErrorCode.ReferenceNotFound] = new(
            ErrorCode.ReferenceNotFound,
            "Module reference not found",
            "The specified module reference could not be found.",
            "Make sure the referenced module exists and that you are specifying the correct path."),
        [ErrorCode.NoFilesFoundForModule] = new(
            ErrorCode.NoFilesFoundForModule,
            "No files found for module",
            "No source files were found for the specified module.",
            "Make sure the module contains at least one source file."),
        [ErrorCode.InvalidCharacter] = new(
            ErrorCode.InvalidCharacter,
            "Invalid character",
            "An invalid character was encountered in the source code.",
            "Check that the source code contains only valid characters."),
        [ErrorCode.UnterminatedStringLiteral] = new(
            ErrorCode.UnterminatedStringLiteral,
            "Unterminated string literal",
            "A string literal was not terminated with a closing quotation mark.",
            "Add a closing quotation mark to the end of the string literal."),
        [ErrorCode.InvalidNumberFormat] = new(
            ErrorCode.InvalidNumberFormat,
            "Invalid number format",
            "A number was not formatted correctly.",
            "Check that the number is formatted correctly."),
        [ErrorCode.UnknownEscapeSequence] = new(
            ErrorCode.UnknownEscapeSequence,
            "Unknown escape sequence",
            "An unknown escape sequence was encountered in the source code.",
            "Check that the escape sequence is valid."),
        [ErrorCode.ExpectedTokenMissing] = new(
            ErrorCode.ExpectedTokenMissing,
            "Expected token missing",
            "A token was expected but was not found.",
            "Check that the source code contains the expected token."),
        [ErrorCode.UnexpectedToken] = new(
            ErrorCode.UnexpectedToken,
            "Unexpected token",
            "An unexpected token was encountered in the source code.",
            "Check that the token is valid for the current context."),
        [ErrorCode.UnexpectedEndOfFile] = new(
            ErrorCode.UnexpectedEndOfFile,
            "File ended unexpectedly",
            "The file was ended while tokens were still expected",
            "Check that the source code is complete and that all scopes are properly closed"),
        [ErrorCode.InvalidVariableDeclaration] = new(
            ErrorCode.InvalidVariableDeclaration,
            "Variable declaration incomplete",
            "A variable declaration was started using `let` but no initializer was provided."),
        [ErrorCode.DuplicateAccessor] = new(
            ErrorCode.DuplicateAccessor,
            "Duplicate accessor",
            "An accessor (getter or setter) was defined more than once for the same property.",
            "Check that each property has at most one getter and one setter.",
            """
            number Age { get; set; } // Valid
            number Wins { get; set; get; } // Invalid - duplicate getter
            """),
        [ErrorCode.UnknownType] = new(
            ErrorCode.UnknownType,
            "Unknown type",
            "Attempted to declare a type that has not been implemented or does not exist in the language",
            "Check that all type definitions follow expected conventions, and that none use types (`interface`, `enum`) that don't exist in the language"),
        [ErrorCode.DuplicateSymbol] = new(
            ErrorCode.DuplicateSymbol,
            "Duplicate symbol",
            "A symbol was declared more than once in the same scope.",
            "Check that each symbol is declared only once in the same scope."),
            [ErrorCode.UnsupportedNode] = new(
                ErrorCode.UnsupportedNode,
                "Unsupported type member node",
                "A definition for a type member (e.g., property, method) is not supported in the current context.",
                "Check that the type member is valid for the current context."),
            [ErrorCode.TypeNotFound] = new(
                ErrorCode.TypeNotFound,
                "Type not found",
                "A type was referenced that could not be found.",
                "Check that the type is defined and that you are referencing it correctly."),
            [ErrorCode.UnableToInferType] = new(
                ErrorCode.UnableToInferType,
                "Unable to infer type",
                "The type of an expression could not be inferred.",
                "Check that the expression has a clear type and that all necessary type information is available."),
            [ErrorCode.IllegalStatement] = new(
                ErrorCode.IllegalStatement,
                "Illegal statement",
                "A statement was used in an illegal context.",
                "Check that the statement is valid for the current context."),
            [ErrorCode.IllegalExpression] = new(
                ErrorCode.IllegalExpression,
                "Illegal expression",
                "An expression was used in an illegal context.",
                "Check that the expression is valid for the current context."),
            [ErrorCode.IdentifierNotFound] = new(
                ErrorCode.IdentifierNotFound,
                "Identifier not found",
                "An identifier (variable, property, method) was referenced that could not be found.",
                "Check that the identifier is defined and that you are referencing it correctly."),
            [ErrorCode.VariableAlreadyDeclared] = new(
                ErrorCode.VariableAlreadyDeclared,
                "Variable already declared",
                "A variable was declared more than once in the same scope.",
                "Check that each variable is declared only once in the same scope."),
            [ErrorCode.InvalidOperator] = new(
                ErrorCode.InvalidOperator,
                "Invalid operator",
                "An operator was used that is not supported for the given operand types.",
                "Check that the operator is valid for the operand types."),
            [ErrorCode.UnsupportedLiteral] = new(
                ErrorCode.UnsupportedLiteral,
                "Unsupported literal",
                "A literal value was used that is not supported in the current context.",
                "Check that the literal is valid for the current context."),
            [ErrorCode.TypeMismatch] = new(
                ErrorCode.TypeMismatch,
                "Type mismatch",
                "An operation was attempted on values of incompatible types.",
                "Check that the types of the values are compatible for the operation."),
            [ErrorCode.TypeNotConstructable] = new(
                ErrorCode.TypeNotConstructable,
                "Type not constructable",
                "An attempt was made to construct a type that cannot be instantiated.",
                "Check that the type can be instantiated and that you are using the correct syntax for construction."),
            [ErrorCode.CannotFindConstructor] = new(
                ErrorCode.CannotFindConstructor,
                "Constructor not found",
                "An attempt was made to construct a type, but no matching constructor could be found.",
                "Check that the type has a constructor that matches the provided arguments."),
            [ErrorCode.IllegalAccess] = new(
                ErrorCode.IllegalAccess,
                "Illegal access",
                "An attempt was made to access a member that is not accessible in the current context.",
                "Check that the member is accessible and that you are referencing it correctly."),
            [ErrorCode.UnknownMember] = new(
                ErrorCode.UnknownMember,
                "Unknown member",
                "An attempt was made to access a member that does not exist on the target type.",
                "Check that the member exists on the target type and that you are referencing it correctly."),
            [ErrorCode.TargetNotCallable] = new(
                ErrorCode.TargetNotCallable,
                "Target not callable",
                "An attempt was made to call a value that is not a function or method.",
                "Check that the target of the call is a function or method and that you are using the correct syntax for calling it.")
    };

    public static bool TryGet(ErrorCode code, out ErrorInfo info) => Map.TryGetValue(code, out info!);

    public static ErrorInfo GetOrDefault(ErrorCode code) =>
        Map.TryGetValue(code, out var info)
            ? info
            : new ErrorInfo(code, code.ToCodeString(), "No detailed explanation is available for this error.");
}    
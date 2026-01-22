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
            "Check that the token is valid for the current context.")
    };

    public static bool TryGet(ErrorCode code, out ErrorInfo info) => Map.TryGetValue(code, out info!);

    public static ErrorInfo GetOrDefault(ErrorCode code) =>
        Map.TryGetValue(code, out var info)
            ? info
            : new ErrorInfo(code, code.ToCodeString(), "No detailed explanation is available for this error.");
}    
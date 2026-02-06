namespace VectraCompiler.Core.Errors;

public enum ErrorCode
{
    // Metadata level errors
    FileNotFound = 0x0001,
    PackageFormatInvalid = 0x0002,
    ModuleNotFound = 0x0003,
    ModuleFormatInvalid = 0x0004,
    CircularDependencyDetected = 0x0005,
    ReferenceNotFound = 0x0006,
    NoFilesFoundForModule = 0x0007,
    // Lexing level errors
    InvalidCharacter = 0x1001,
    UnterminatedStringLiteral = 0x1002,
    InvalidNumberFormat = 0x1003,
    UnknownEscapeSequence = 0x1004,
    // Parsing level errors
    ExpectedTokenMissing = 0x2001,
    UnexpectedToken = 0x2002,
    UnexpectedEndOfFile = 0x2003,
    InvalidVariableDeclaration = 0x2004,
    DuplicateAccessor = 0x2005,
    UnknownType = 0x2006,
    //Binding Errors
    DuplicateSymbol = 0x3001,
    UnsupportedNode = 0x3002,
    TypeNotFound = 0x3003,
    // CodeGen errors
}

public static class ErrorCodeExtensions
{
    public static string ToCodeString(this ErrorCode code)
        => $"VEC{(int)code:X4}";

    public static bool TryParseCodeString(this string codeString, out ErrorCode errorCode)
    {
        errorCode = default;
        if (codeString.IsNullOrWhiteSpace())
            return false;

        codeString = codeString.Trim();

        if (!codeString.StartsWith("VEC", StringComparison.OrdinalIgnoreCase))
            return false;
        var hexPart = codeString[3..];
        if (!int.TryParse(hexPart,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out var codeNumber))
            return false;
        if (!Enum.IsDefined(typeof(ErrorCode), codeNumber))
            return false;
        errorCode = (ErrorCode)codeNumber;
        return true;
    }
}
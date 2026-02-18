namespace VectraCompiler.Core;

public record SourceSpan(int StartLine, int StartColumn, int EndLine, int EndColumn, string? FilePath = null)
{
    public SourceSpan(TokenPosition start, TokenPosition end, string? filePath = null) 
        : this(start.Line, start.Column, end.Line, end.Column, filePath)
    {
    }
    
    public static SourceSpan EmptyAtStart => new SourceSpan(0, 0, 0, 0);
}

public record TokenPosition(int Line, int Column);
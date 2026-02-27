namespace VectraCompiler.Core;

public record SourceSpan(int StartLine, int StartColumn, int StartOffset, int EndLine, int EndColumn, int EndOffset, string? FilePath = null)
{
    public SourceSpan(TokenPosition start, TokenPosition end, string? filePath = null) 
        : this(start.Line, start.Column, start.Offset, end.Line, end.Column, end.Offset, filePath)
    {
    }
    
    public static SourceSpan EmptyAtStart => new(0, 0, 0, 0, 0, 0);

    public SourceSpan(int StartLine, int StartColumn, int EndLine, int EndColumn, string? filePath = null)
        : this(StartLine, StartColumn, 0, EndLine, EndColumn, 0, filePath)
    {
    }
}

public record TokenPosition(int Line, int Column, int Offset);
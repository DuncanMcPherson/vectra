namespace VectraCompiler.Core;

public record SourceSpan(int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public SourceSpan(TokenPosition start, TokenPosition end) : this(start.Line, start.Column, end.Line, end.Column)
    {
    }
    
    public static SourceSpan EmptyAtStart => new SourceSpan(0, 0, 0, 0);
}

public record TokenPosition(int Line, int Column);
using VectraCompiler.AST.Lexing.Models;

namespace VectraCompiler.AST.Models;

/// <summary>
/// Represents a source code span with specific start and end positions in a source file.
/// This record is used to define the location of a syntactic or semantic element
/// within the source code being compiled.
/// </summary>
public record SourceSpan(int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public SourceSpan(TokenPosition start, TokenPosition end) : this(start.Line, start.Column, end.Line, end.Column)
    {
    }

    public static SourceSpan EmptyAtStart => new SourceSpan(0, 0, 0, 0);
}
using VectraCompiler.Core;

namespace VectraCompiler.AST.Lexing.Models;

public class Token(TokenType type, string value, SourceSpan span)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public SourceSpan Span { get; } = span;
    public TokenPosition Position => new(Span.StartLine, Span.StartColumn, Span.StartOffset);

    public override string ToString()
    {
        return $"({Type}) \"{Value}\": (Line: {Span.StartLine}, Column: {Span.StartColumn})";
    }
}

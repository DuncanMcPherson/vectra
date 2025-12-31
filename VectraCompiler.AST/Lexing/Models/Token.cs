namespace VectraCompiler.AST.Lexing.Models;

public class Token(TokenType type, string value, TokenPosition position)
{
    public TokenType Type { get; } = type;
    public string Value { get; } = value;
    public TokenPosition Position { get; } = position;

    public override string ToString()
    {
        return $"({Type}) \"{Value}\": (Line: {Position.Line}, Column: {Position.Column})";
    }
}

public record TokenPosition(int Line, int Column);
namespace VectraCompiler.AST.Lexing.Models;

public enum TokenType
{
    Identifier,
    Number,
    String,
    Keyword,
    Symbol,
    Operator,
    CollectionOperator,
    EndOfFile
}
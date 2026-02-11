using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Models;

public sealed class BodyBindResult
{
    public required DeclarationBindResult Declarations { get; init; }
    public required Dictionary<Symbol, BoundBlockStatement> BodiesByMember { get; init; }
}
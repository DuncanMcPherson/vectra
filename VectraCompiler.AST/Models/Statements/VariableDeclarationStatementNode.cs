using VectraCompiler.AST.Models.Expressions;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Statements;

public class VariableDeclarationStatementNode(string name, string? explicitType, IExpressionNode? initializer, SourceSpan span) : AstNodeBase, IStatementNode
{
    public override SourceSpan Span { get; } = span;
    public string Name { get; } = name;
    public string? ExplicitType { get; } = explicitType;
    public IExpressionNode? Initializer { get; } = initializer;

    public override string ToPrintable()
    {
        return $"{ExplicitType ?? "let"} {Name}{(Initializer != null ? $" = {Initializer}" : string.Empty)};";
    }
}
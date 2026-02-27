using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class EnterDirectiveNode(string spaceName, SourceSpan span) : AstNodeBase, IAstNode
{
    public string SpaceName { get; } = spaceName;
    public override SourceSpan Span { get; } = span;
    
    public override string ToPrintable() => $"enter {SpaceName}";
}
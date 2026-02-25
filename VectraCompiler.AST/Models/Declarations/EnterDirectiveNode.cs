using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations;

public class EnterDirectiveNode(string spaceName, SourceSpan span) : IAstNode
{
    public string SpaceName { get; } = spaceName;
    public SourceSpan Span { get; } = span;
    
    public string ToPrintable() => $"enter {SpaceName}";
}
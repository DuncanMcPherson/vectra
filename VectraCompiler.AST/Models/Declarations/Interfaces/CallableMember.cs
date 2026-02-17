using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations.Interfaces;

public abstract class CallableMember : AstNodeBase, IMemberNode
{
    public abstract string Name { get; }
    public abstract IList<VParameter> Parameters { get; }
    public abstract BlockStatementNode Body { get; }
    public abstract override SourceSpan Span { get; }
    public abstract override T Visit<T>(IAstVisitor<T> visitor);
    
    public abstract override string ToPrintable();
}
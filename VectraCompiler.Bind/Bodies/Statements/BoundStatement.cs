using VectraCompiler.AST.Models;

namespace VectraCompiler.Bind.Bodies.Statements;

public abstract class BoundStatement(SourceSpan span) : BoundNode(span);


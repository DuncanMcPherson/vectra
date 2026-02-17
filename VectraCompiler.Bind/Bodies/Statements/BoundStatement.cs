using VectraCompiler.AST.Models;
using VectraCompiler.Core;

namespace VectraCompiler.Bind.Bodies.Statements;

public abstract class BoundStatement(SourceSpan span) : BoundNode(span);


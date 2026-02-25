using VectraCompiler.Core;

namespace VectraCompiler.AST.Models;

/// <summary>
/// Represents a node in the abstract syntax tree (AST) structure.
/// This interface defines the contract for all AST nodes within the compiler.
/// </summary>
public interface IAstNode
{
    /// Represents the span of source code in terms of its start and end positions,
    /// including line and column numbers. This property provides information about
    /// the location of the corresponding Abstract Syntax Tree (AST) node in the source code.
    SourceSpan Span { get; }
    string ToPrintable();
}

public abstract class AstNodeBase : IAstNode
{
    public abstract SourceSpan Span { get; }
    public abstract string ToPrintable();
    public override string ToString() => ToPrintable();
}
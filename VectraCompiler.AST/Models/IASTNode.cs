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

    /// <summary>
    /// Accepts a visitor object to perform operations on this AST node.
    /// This is part of the visitor design pattern implementation.
    /// </summary>
    /// <typeparam name="T">The return type of the operation defined in the visitor.</typeparam>
    /// <param name="visitor">The visitor instance that defines the operations performed on the node.</param>
    /// <returns>The result of the operation performed by the visitor.</returns>
    T Visit<T>(IAstVisitor<T> visitor);

    string ToPrintable();
}

public abstract class AstNodeBase : IAstNode
{
    public abstract SourceSpan Span { get; }
    public abstract T Visit<T>(IAstVisitor<T> visitor);
    public abstract string ToPrintable();
    public override string ToString() => ToPrintable();
}
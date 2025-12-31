namespace VectraCompiler.AST.Models.Declarations.Interfaces;

/// <summary>
/// Defines a contract for a member node in the abstract syntax tree (AST).
/// This interface represents a named entity within the AST structure.
/// </summary>
public interface IMemberNode : IAstNode
{
    /// <summary>
    /// Gets the identifier or label associated with an instance of an object implementing the <see cref="IMemberNode"/> interface.
    /// </summary>
    string Name { get; }
}
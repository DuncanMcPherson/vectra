namespace VectraCompiler.AST.Models.Declarations.Interfaces;

/// <summary>
/// Represents a type declaration node in the abstract syntax tree (AST) structure.
/// This interface is designed to define the essential behavior and properties
/// for type declaration nodes within the compiler's type system.
/// </summary>
public interface ITypeDeclarationNode : IAstNode
{
    /// <summary>
    /// Gets the name of the type declaration node.
    /// This property is used to retrieve the identifier associated with the type
    /// being declared in the abstract syntax tree (AST).
    /// </summary>
    string Name { get; }
    // TODO: add space references so that I can traverse spaces in both directions
}
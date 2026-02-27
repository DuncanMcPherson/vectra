using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Bind.Models;

public class DeclarationBindResult
{
    public ImportedSpaceContext ImportedSpaces { get; init; } = new();
    public required Scope PackageScope { get; init; }
    public required Dictionary<IAstNode, Symbol> SymbolsByNode { get; init; }
    public required Dictionary<NamedTypeSymbol, Scope> TypeMemberScopes { get; init; }
    public required Dictionary<(string ModuleName, string QualifiedName), Scope> SpaceScopesByFullName { get; init; }
    public required Dictionary<IMemberNode, NamedTypeSymbol> ContainingTypeByNode { get; init; }
    public required Dictionary<NamedTypeSymbol, ITypeDeclarationNode> TypeNodesBySymbol { get; init; }
}
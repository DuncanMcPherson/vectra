using VectraCompiler.AST.Models;

namespace VectraCompiler.Bind.Models;

public class DeclarationBindResult
{
    public required Scope PackageScope { get; init; }
    public required Dictionary<IAstNode, Symbol> SymbolsByNode { get; init; }
    public required Dictionary<NamedTypeSymbol, Scope> TypeMemberScopes { get; init; }
    public required Dictionary<(string ModuleName, string QualifiedName), Scope> SpaceScopesByFullName { get; init; }
}
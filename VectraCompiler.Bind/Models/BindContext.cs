using VectraCompiler.AST.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Bind.Models;

public sealed record BindContext
{
    public required DeclarationBindResult Declarations { get; init; }
    public required DiagnosticBag Diagnostics { get; init; }
    public required Scope Scope { get; init; }
    public NamedTypeSymbol? ContainingType { get; init; }
    public MethodSymbol? ContainingMethod { get; init; }
    public TypeSymbol? ExpectedType { get; init; }
    public bool IsLValueTarget { get; init; }
    public BindContext WithScope(Scope scope) => this with { Scope = scope };
    public BindContext WithExpectedType(TypeSymbol? expected) => this with { ExpectedType = expected };
    public BindContext WithLValueTarget(bool isTarget) => this with { IsLValueTarget = isTarget };

    public BindContext WithContainingType(NamedTypeSymbol? type) => this with { ContainingType = type };
    public BindContext WithContainingFunction(MethodSymbol? fn) => this with { ContainingMethod = fn };

    /// <summary>Convenience: look up the declaration symbol bound to an AST node from pass 1.</summary>
    public bool TryGetDeclaredSymbol(IAstNode node, out Symbol symbol)
        => Declarations.SymbolsByNode.TryGetValue(node, out symbol!);

    /// <summary>Convenience: get the member scope for a declared type.</summary>
    public bool TryGetMemberScope(NamedTypeSymbol type, out Scope scope)
        => Declarations.TypeMemberScopes.TryGetValue(type, out scope!);
}
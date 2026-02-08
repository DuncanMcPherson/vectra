using VectraCompiler.AST.Models.Statements;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;

namespace VectraCompiler.Bind;

public class BinderService
{
    public BoundBlockStatement BindMethodBody(
        DeclarationBindResult decls,
        MethodSymbol method,
        NamedTypeSymbol containingType,
        BlockStatementNode body,
        DiagnosticBag diagnostics)
    {
        decls.TypeMemberScopes.TryGetValue(containingType, out var memberScope);
        var localScope = new Scope(memberScope);

        var ctx = new BindContext
        {
            Declarations = decls,
            Diagnostics = diagnostics,
            Scope = localScope,
            ContainingType = containingType,
            ContainingMethod = method,
            ExpectedType = method.ReturnType,
            IsLValueTarget = false
        };
        return null!;
    }
}
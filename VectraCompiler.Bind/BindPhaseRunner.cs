using Spectre.Console;
using VectraCompiler.AST;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Bind.Models;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;

namespace VectraCompiler.Bind;

public static class BindPhaseRunner
{
    private static readonly Dictionary<(Scope Parent, string Name), Scope> SpaceScopes = [];
    public static async Task<Result<DeclarationBindResult>> RunAsync(
        VectraAstPackage package,
        CancellationToken ct = default)
    {
        SpaceScopes.Clear();
        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn(), new ElapsedTimeMsColumn())
            .StartAsync(async ctx =>
            {
                ct.ThrowIfCancellationRequested();
                var db = new DiagnosticBag();
                using var _ = Logger.BeginPhase(CompilePhase.Bind, "Starting binding");
                var task = ctx.AddTask("Bind Symbols", maxValue: package.Modules.Count);

                var packageScope = new Scope(null);
                DeclareBuiltIns(packageScope);

                var symbolsByNode = new Dictionary<IAstNode, Symbol>();
                var typeMemberScopes = new Dictionary<NamedTypeSymbol, Scope>();
                var spacesByName = new Dictionary<(string ModuleName, string QualifiedName), Scope>();

                foreach (var module in package.Modules)
                {
                    var moduleScope = new Scope(packageScope);

                    foreach (var file in module.Files.Select(p => p.Tree))
                    {
                        BindSpaceDeclaration(moduleScope, file.Space, module.ModuleName, symbolsByNode, typeMemberScopes, spacesByName, db);
                    }
                    task.Increment(1);
                }
                task.StopTask();
                if (db.Items.Count(x => x.Severity == Severity.Error) > 0)
                {
                    foreach (var item in db.Items.Where(d => d.Severity == Severity.Error))
                    {
                        Logger.LogError($"[{item.CodeString}] {item.Message}");
                    }
                    return Result<DeclarationBindResult>.Fail(db);
                }

                return Result<DeclarationBindResult>.Pass(new DeclarationBindResult
                {
                    PackageScope = packageScope,
                    SymbolsByNode = symbolsByNode,
                    TypeMemberScopes = typeMemberScopes,
                    SpaceScopesByFullName = spacesByName
                }, db);
            });
    }

    private static void BindSpaceDeclaration(
        Scope parentScope,
        SpaceDeclarationNode space,
        string moduleName,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes,
        Dictionary<(string ModuleName, string QualifiedName), Scope> spacesByName,
        DiagnosticBag db)
    {
        var spaceScope = GetOrCreateSpaceScope(parentScope, space.Name);
        var scopeKey = (moduleName, space.QualifiedName);
        spacesByName.TryAdd(scopeKey, spaceScope);
        foreach (var declaration in space.Declarations)
        {
            BindDeclaration(spaceScope, declaration, space.QualifiedName, symbolsByNode, typeMemberScopes, db);
        }

        foreach (var subspace in space.Subspaces)
        {
            BindSpaceDeclaration(spaceScope, subspace, moduleName, symbolsByNode, typeMemberScopes, spacesByName, db);
        }

        foreach (var declaration in space.Declarations)
        {
            BindMember(declaration, symbolsByNode, typeMemberScopes, db);
        }
    }

    private static void BindMember(ITypeDeclarationNode type, Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes, DiagnosticBag db)
    {
        // Soon we will have multiple types of type declaration nodes, so we will switch on the type to determine how to bind members
        if (type is not ClassDeclarationNode @class || !symbolsByNode.TryGetValue(type, out var sym) || sym is not NamedTypeSymbol typeSym)
            return;
        var memberScope = typeMemberScopes[typeSym];

        foreach (var member in @class.Members)
        {
            switch (member)
            {
                case PropertyDeclarationNode pdn:
                    // No Symbol defined yet, skipping for now
                    break;
                case FieldDeclarationNode fdn:
                    // No Symbol defined yet, skipping for now
                    break;
                case ConstructorDeclarationNode cdn:
                    // No Symbol defined yet, skipping for now
                    break;
                case MethodDeclarationNode mdn:
                    BindFunction(memberScope, mdn, symbolsByNode, typeSym, db);
                    break;
                default:
                    db.Error(ErrorCode.UnsupportedNode, $"Unsupported member declaration of type '{member.GetType().Name}'");
                    break;
            }
        }
    }

    private static void BindFunction(Scope scope, MethodDeclarationNode method, Dictionary<IAstNode, Symbol> symbolsByNode,
        NamedTypeSymbol typeSym, DiagnosticBag db)
    {
        var funcName = method.Name;
        var returnType = ResolveType(scope, method.ReturnType, db);
        var parameters = BindParameters(scope, typeSym, method.Parameters, db);
        var funcSym = new FunctionSymbol(funcName, returnType, parameters);
        if (!scope.TryDeclare(funcSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Function {funcName} is already declared in {typeSym.Name}");
            return;
        }

        symbolsByNode[method] = funcSym;
    }

    private static void BindDeclaration(Scope parentScope, ITypeDeclarationNode type, string parentFullName,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes, DiagnosticBag db)
    {
        var fullName = $"{parentFullName}.{type.Name}";
        var typeSym = new NamedTypeSymbol(type.Name, fullName, NamedTypeKind.Class);
        if (!parentScope.TryDeclare(typeSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Duplicate type symbol '{fullName}'");
            return;
        }

        symbolsByNode[type] = typeSym;
        var memberScope = new Scope(parentScope);
        typeMemberScopes[typeSym] = memberScope;
    }

    private static Scope GetOrCreateSpaceScope(Scope parent, string name)
    {
        var key = (parent, name);
        if (SpaceScopes.TryGetValue(key, out var existing))
            return existing;

        var created = new Scope(parent);
        SpaceScopes[key] = created;
        return created;
    }

    private static void DeclareBuiltIns(Scope packageScope)
    {
        packageScope.TryDeclare(BuiltInTypeSymbol.Void);
        packageScope.TryDeclare(BuiltInTypeSymbol.String);
        packageScope.TryDeclare(BuiltInTypeSymbol.Number);
        packageScope.TryDeclare(BuiltInTypeSymbol.Bool);
        packageScope.TryDeclare(BuiltInTypeSymbol.Error);
    }

    private static TypeSymbol ResolveType(Scope lookupScope, string typeName, DiagnosticBag db)
    {
        var matches = lookupScope.Lookup(typeName);
        var type = matches.OfType<TypeSymbol>().FirstOrDefault();
        if (type is null)
        {
            db.Error(ErrorCode.TypeNotFound, $"Unknown type '{typeName}'");
            return BuiltInTypeSymbol.Error;
        }
        return type;
    }

    private static IReadOnlyList<ParameterSymbol> BindParameters(
        Scope lookupScope,
        NamedTypeSymbol? declaringType,
        IList<VParameter> parameters,
        DiagnosticBag db)
    {
        var list = new List<ParameterSymbol>(parameters.Count + 1);
        var ordinal = 0;
        if (declaringType is not null)
            list.Add(new ParameterSymbol("this", declaringType, ordinal++));

        list.AddRange(from p in parameters let pType = ResolveType(lookupScope, p.Type, db) let pName = p.Name select new ParameterSymbol(pName, pType, ordinal++));

        return list;
    }
}
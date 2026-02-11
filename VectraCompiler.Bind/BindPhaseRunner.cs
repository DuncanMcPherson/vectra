using Spectre.Console;
using VectraCompiler.AST;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;

namespace VectraCompiler.Bind;

public static class BindPhaseRunner
{
    public static async Task<Result<BodyBindResult>> RunInitialBindingAsync(
        VectraAstPackage package,
        CancellationToken ct = default)
    {
        Dictionary<(Scope Parent, string Name), Scope> spaceScopes = [];
        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn { Alignment = Justify.Left }, new ProgressBarColumn(),
                new PercentageColumn(), new SpinnerColumn(), new ElapsedTimeMsColumn())
            .StartAsync(ctx =>
            {
                ct.ThrowIfCancellationRequested();
                var db = new DiagnosticBag();
                using var _ = Logger.BeginPhase(CompilePhase.Bind, "Starting binding");
                var task = ctx.AddTask("Bind Symbols (Types and Spaces)", maxValue: package.Modules.Count);

                var packageScope = new Scope(null);
                DeclareBuiltIns(packageScope);

                var symbolsByNode = new Dictionary<IAstNode, Symbol>();
                var typeMemberScopes = new Dictionary<NamedTypeSymbol, Scope>();
                var spacesByName = new Dictionary<(string ModuleName, string QualifiedName), Scope>();
                var typeNodesBySymbol = new Dictionary<NamedTypeSymbol, ITypeDeclarationNode>();
                var containingTypeByNode = new Dictionary<IMemberNode, NamedTypeSymbol>();

                foreach (var module in package.Modules)
                {
                    ct.ThrowIfCancellationRequested();
                    var moduleScope = new Scope(packageScope);

                    foreach (var file in module.Files.Select(p => p.Tree))
                    {
                        ct.ThrowIfCancellationRequested();
                        BindSpaceDeclaration(moduleScope, file.Space, module.ModuleName, symbolsByNode, typeMemberScopes, spacesByName, db, spaceScopes, typeNodesBySymbol);
                    }
                    task.Increment(1);
                }
                task.StopTask();
                var membersTask = ctx.AddTask("Bind Symbols (Type Members)", maxValue: typeMemberScopes.Count);
                foreach (var kvp in typeMemberScopes)
                {
                    var (typeSym, _) = kvp;
                    var typeNode = symbolsByNode.First(sbn => sbn.Value == typeSym).Key;
                    if (typeNode is not ITypeDeclarationNode decl)
                        continue;
                    BindMember(decl, symbolsByNode, typeMemberScopes, db, containingTypeByNode);
                    membersTask.Increment(1);
                }
                membersTask.StopTask();
                if (db.Items.Any(x => x.Severity == Severity.Error))
                {
                    return Task.FromResult(Result<BodyBindResult>.Fail(db));
                }

                var declarations = new DeclarationBindResult
                {
                    PackageScope = packageScope,
                    SymbolsByNode = symbolsByNode,
                    TypeMemberScopes = typeMemberScopes,
                    SpaceScopesByFullName = spacesByName
                };
                var binder = new BinderService(declarations, db);

                var bodies = new Dictionary<Symbol, BoundBlockStatement>();
                var bodyTask = ctx.AddTask("Bind Symbols (Method and Constructor Bodies)",
                    maxValue: typeNodesBySymbol.Count);
                foreach (var (typeSymbol, typeNode) in typeNodesBySymbol)
                {
                    if (typeNode is not ClassDeclarationNode cdn)
                        continue;
                    foreach (var memberNode in cdn.Members.OfType<CallableMember>())
                    {
                        if (!declarations.SymbolsByNode.TryGetValue(memberNode, out var sym))
                            continue;
                        switch (sym)
                        {
                            case MethodSymbol m when memberNode is MethodDeclarationNode mdn:
                                Logger.LogTrace($"Binding method body for {mdn.Name} in type {typeSymbol.Name}");
                                bodies[sym] = binder.BindMethodBody(m, typeSymbol, mdn.Body);
                                break;
                            case ConstructorSymbol c when memberNode is ConstructorDeclarationNode cn:
                                Logger.LogTrace($"Binding constructor body for {typeSymbol.Name}");
                                bodies[sym] = binder.BindConstructorBody(c, typeSymbol, cn.Body);
                                break;
                        }
                    }
                    bodyTask.Increment(1);
                }
                bodyTask.StopTask();
                
                var bodyBindResult = new BodyBindResult
                {
                    BodiesByMember = bodies,
                    Declarations = declarations
                };
                var errorCount = db.Items.Count(x => x.Severity == Severity.Error);
                Logger.LogInfo($"Binding completed with {errorCount} errors.");
                var result = db.HasErrors ? Result<BodyBindResult>.Fail(db) : Result<BodyBindResult>.Pass(bodyBindResult, db);
                return Task.FromResult(result);
            });
    }

    private static void BindSpaceDeclaration(
        Scope parentScope,
        SpaceDeclarationNode space,
        string moduleName,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes,
        Dictionary<(string ModuleName, string QualifiedName), Scope> spacesByName,
        DiagnosticBag db,
        Dictionary<(Scope Parent, string Name), Scope> spaceScopes,
        Dictionary<NamedTypeSymbol, ITypeDeclarationNode> typeNodesBySymbol)
    {
        var spaceScope = GetOrCreateSpaceScope(parentScope, space.Name, spaceScopes);
        var scopeKey = (moduleName, space.QualifiedName);
        spacesByName.TryAdd(scopeKey, spaceScope);
        foreach (var declaration in space.Declarations)
        {
            BindDeclaration(spaceScope, declaration, space.QualifiedName, symbolsByNode, typeMemberScopes, db, typeNodesBySymbol);
        }

        foreach (var subspace in space.Subspaces)
        {
            BindSpaceDeclaration(spaceScope, subspace, moduleName, symbolsByNode, typeMemberScopes, spacesByName, db, spaceScopes, typeNodesBySymbol);
        }
    }

    private static void BindMember(ITypeDeclarationNode type, Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes, DiagnosticBag db, Dictionary<IMemberNode, NamedTypeSymbol> containingTypeByMemberNode)
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
                    Logger.LogTrace($"Binding property '{pdn.Name}' in type '{typeSym.Name}'");
                    var propType = ResolveType(memberScope, pdn.Type, db);
                    var propSym = new PropertySymbol(pdn.Name, propType, pdn.HasGetter, pdn.HasSetter);
                    if (!memberScope.TryDeclare(propSym))
                        // TODO: add access to the file name that is currently being bound
                        db.Error(ErrorCode.DuplicateSymbol, $"Property {pdn.Name} is already declared in {typeSym.Name}");
                    symbolsByNode[pdn] = propSym;
                    break;
                case FieldDeclarationNode fdn:
                    Logger.LogTrace($"Binding field '{fdn.Name}' in type '{typeSym.Name}'");
                    var fieldType = ResolveType(memberScope, fdn.Type, db);
                    var fieldSym = new FieldSymbol(fdn.Name, fieldType);
                    if (!memberScope.TryDeclare(fieldSym))
                        db.Error(ErrorCode.DuplicateSymbol, $"Field {fdn.Name} is already declared in {typeSym.Name}");
                    symbolsByNode[fdn] = fieldSym;
                    break;
                case ConstructorDeclarationNode cdn:
                    Logger.LogTrace($"Binding constructor in type '{typeSym.Name}'");
                    var parameters = BindParameters(memberScope, typeSym, cdn.Parameters, db);
                    var ctorSym = new ConstructorSymbol(typeSym, parameters);
                    if (!memberScope.TryDeclare(ctorSym))
                        db.Error(ErrorCode.DuplicateSymbol, $"Constructor is already declared in {typeSym.Name}({ctorSym.Arity})");
                    symbolsByNode[cdn] = ctorSym;
                    containingTypeByMemberNode[cdn] = typeSym;
                    break;
                case MethodDeclarationNode mdn:
                    Logger.LogTrace($"Binding method '{mdn.Name}' in type '{typeSym.Name}'");
                    BindFunction(memberScope, mdn, symbolsByNode, typeSym, db);
                    containingTypeByMemberNode[mdn] = typeSym;
                    break;
                default:
                    db.Error(ErrorCode.UnsupportedNode, $"Unsupported member declaration of type '{member.GetType().Name}'");
                    break;
            }
        }
        
        var ctors = @class.Members.OfType<ConstructorDeclarationNode>();
        if (ctors.Any()) return;
        var defaultCtor = new ConstructorSymbol(typeSym, BindParameters(memberScope, typeSym, new List<VParameter>(), db));
        memberScope.TryDeclare(defaultCtor);
    }

    private static void BindFunction(Scope scope, MethodDeclarationNode method, Dictionary<IAstNode, Symbol> symbolsByNode,
        NamedTypeSymbol typeSym, DiagnosticBag db)
    {
        var funcName = method.Name;
        var returnType = ResolveType(scope, method.ReturnType, db);
        Logger.LogTrace($"Return type of function '{funcName}' is '{returnType.Name}'");
        var parameters = BindParameters(scope, typeSym, method.Parameters, db);
        var funcSym = new MethodSymbol(funcName, returnType, parameters);
        if (!scope.TryDeclare(funcSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Function {funcName} is already declared in {typeSym.Name}");
            return;
        }

        symbolsByNode[method] = funcSym;
    }

    private static void BindDeclaration(Scope parentScope, ITypeDeclarationNode type, string parentFullName,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes, DiagnosticBag db,
        Dictionary<NamedTypeSymbol, ITypeDeclarationNode> typeNodesBySymbol)
    {
        Logger.LogTrace($"Binding declaration '{type.Name}' in scope '{parentFullName}'");
        var fullName = $"{parentFullName}.{type.Name}";
        var typeSym = new NamedTypeSymbol(type.Name, fullName, NamedTypeKind.Class);
        typeNodesBySymbol[typeSym] = type;
        if (!parentScope.TryDeclare(typeSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Duplicate type symbol '{fullName}'");
            return;
        }

        symbolsByNode[type] = typeSym;
        var memberScope = new Scope(parentScope);
        Logger.LogTrace($"Created member scope for type '{fullName}'");
        typeMemberScopes[typeSym] = memberScope;
    }

    private static Scope GetOrCreateSpaceScope(Scope parent, string name, Dictionary<(Scope Parent, string Name), Scope> spaceScopes)
    {
        Logger.LogTrace($"Creating scope for space '{name}'");
        var key = (parent, name);
        if (spaceScopes.TryGetValue(key, out var existing))
        {
            Logger.LogTrace($"Reusing existing scope for space '{name}'");
            return existing;
        }

        var created = new Scope(parent);
        Logger.LogTrace($"Created new scope for space '{name}'");
        spaceScopes[key] = created;
        return created;
    }

    private static void DeclareBuiltIns(Scope packageScope)
    {
        Logger.LogTrace("Declaring built-in types");
        packageScope.TryDeclare(BuiltInTypeSymbol.Void);
        packageScope.TryDeclare(BuiltInTypeSymbol.String);
        packageScope.TryDeclare(BuiltInTypeSymbol.Number);
        packageScope.TryDeclare(BuiltInTypeSymbol.Bool);
        packageScope.TryDeclare(BuiltInTypeSymbol.Null);
        packageScope.TryDeclare(BuiltInTypeSymbol.Error);
        packageScope.TryDeclare(BuiltInTypeSymbol.Unknown);
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
        {
            list.Add(new ParameterSymbol("this", declaringType, ordinal++));
            Logger.LogTrace($"Adding 'this' parameter for type '{declaringType.Name}' at index 0");
        }

        if (parameters.Any(p => p.Name == "this"))
            db.Error(ErrorCode.DuplicateSymbol, "Parameter 'this' is not allowed in methods");

        foreach (var p in parameters)
        {
            if (p.Name == "this") continue;
            var pType = ResolveType(lookupScope, p.Type, db);
            var pName = p.Name;
            Logger.LogTrace($"Binding parameter '{pName}' of type '{pType.Name}', at index {ordinal}");
            list.Add(new ParameterSymbol(pName, pType, ordinal++));
        }

        return list;
    }
}
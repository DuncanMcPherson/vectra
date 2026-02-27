using VectraCompiler.AST;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations;
using VectraCompiler.AST.Models.Declarations.Interfaces;
using VectraCompiler.Bind.Models;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;

namespace VectraCompiler.Bind;

/// <summary>
/// Performs the binding phase. Can be called independently, from external tools such as the LSP
/// </summary>
public sealed class Binder
{
    private readonly Dictionary<(Scope Parent, string Name), Scope> _spaceScopes = new();

    /// <summary>
    /// Binds type declarations within the provided compiler package, resolving symbols, creating scopes,
    /// and associating diagnostics. This is a key phase in the compilation process that ensures proper
    /// symbol resolution and scope management for the types defined in the source files.
    /// </summary>
    /// <param name="package">The compiler package containing modules and files to process during binding.</param>
    /// <param name="packageScope">The root scope for the package, which serves as the starting point for scope generation.</param>
    /// <param name="result">The data structure to store binding results, including symbol and scope mappings.</param>
    /// <param name="db">The diagnostic bag used to collect errors and warnings generated during the binding phase.</param>
    public void BindTypes(
        VectraAstPackage package,
        Scope packageScope,
        DeclarationBindResult result,
        DiagnosticBag db)
    {
        DeclareBuiltIns(packageScope);

        foreach (var module in package.Modules)
        {
            var moduleScope = new Scope(packageScope);
            foreach (var file in module.Files.Select(p => p.Tree))
            {
                BindSpaceDeclaration(
                    moduleScope,
                    file.Space,
                    module.ModuleName,
                    file.FilePath,
                    result.SymbolsByNode,
                    result.TypeMemberScopes,
                    result.SpaceScopesByFullName,
                    db,
                    result.TypeNodesBySymbol);
            }

            foreach (var file in module.Files.Select(p => p.Tree))
            {
                foreach (var directive in file.EnterDirectives)
                {
                    var matches = result.SpaceScopesByFullName
                        .Where(kvp => kvp.Key.QualifiedName == directive.SpaceName)
                        .Select(kvp => kvp.Value)
                        .ToList();
                    if (matches.Count < 1)
                    {
                        db.Error(ErrorCode.SpaceNotFound, $"Cannot find space '{directive.SpaceName}' to import");
                        continue;
                    }

                    foreach (var scope in matches)
                    {
                        result.ImportedSpaces.AddImport(file.FilePath, scope);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Binds members for the provided type nodes, linking them to their enclosing types, resolving symbols,
    /// and associating diagnostics. This ensures that members are properly connected to their scope and
    /// that any issues during the binding process are reported.
    /// </summary>
    /// <param name="result">The data structure containing the results of the binding phases, including symbols, scopes, and type-node mappings.</param>
    /// <param name="db">The diagnostic bag used for collecting errors and warnings generated during the member binding phase.</param>
    public void BindMembers(
        DeclarationBindResult result,
        DiagnosticBag db)
    {
        foreach (var (typeSym, typeNode) in result.TypeNodesBySymbol)
        {
            BindMember(typeNode,
                typeSym.DeclarationSpan?.FilePath!,
                result.SymbolsByNode,
                result.TypeMemberScopes,
                db,
                result.ContainingTypeByNode);
        }
    }

    /// <summary>
    /// Binds a space declaration by creating or retrieving its associated scope, registering it within
    /// the context of the current compilation session, and processing its declarations and subspaces.
    /// </summary>
    /// <param name="parentScope">The parent scope under which the current space scope is nested.</param>
    /// <param name="space">The space declaration node representing the space to be bound.</param>
    /// <param name="moduleName">The name of the module containing the space declaration.</param>
    /// <param name="filePath">The file path where the space declaration is located.</param>
    /// <param name="symbolsByNode">A mapping of AST nodes to their corresponding symbols for reference and symbol resolution.</param>
    /// <param name="typeMemberScopes">A mapping of named type symbols to their associated member scopes.</param>
    /// <param name="spacesByName">A dictionary mapping fully qualified space names to their corresponding scopes for lookup and management.</param>
    /// <param name="db">The diagnostic bag used for reporting errors and warnings encountered during the binding process.</param>
    /// <param name="typeScopes">A mapping of named type symbols to their associated type declaration nodes.</param>
    private void BindSpaceDeclaration(
        Scope parentScope,
        SpaceDeclarationNode space,
        string moduleName,
        string filePath,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes,
        Dictionary<(string ModuleName, string QualifiedName), Scope> spacesByName,
        DiagnosticBag db,
        Dictionary<NamedTypeSymbol, ITypeDeclarationNode> typeScopes)
    {
        var spaceScope = GetOrCreateSpaceScope(parentScope, space.Name);
        spacesByName.TryAdd((moduleName, space.QualifiedName), spaceScope);
        
        foreach (var declaration in space.Declarations)
            BindDeclaration(spaceScope, declaration, space.QualifiedName, filePath, symbolsByNode, typeMemberScopes, db, typeScopes);

        foreach (var subspace in space.Subspaces)
            BindSpaceDeclaration(spaceScope, subspace, moduleName, filePath, symbolsByNode, typeMemberScopes, spacesByName, db, typeScopes);
    }

    /// <summary>
    /// Processes the binding of a type declaration by associating it with a corresponding symbol,
    /// adding it to the appropriate scope, and managing metadata such as file locations and diagnostics.
    /// This function ensures that the symbol for the type is properly resolved and tracked within the compiler's current context.
    /// </summary>
    /// <param name="parentScope">The parent scope in which the type declaration is being bound. It is used to declare the new type and create a new child scope if successful.</param>
    /// <param name="type">The type declaration node representing the type being processed. Contains the structural and semantic information used during the binding phase.</param>
    /// <param name="parentFullName">The fully qualified name of the parent scope or namespace. Provides the root for concatenating the new type's fully qualified name.</param>
    /// <param name="filePath">The path to the source file where the type is declared. Used for associating diagnostics and metadata with this declaration.</param>
    /// <param name="symbolsByNode">A mapping of AST nodes to their corresponding symbols. Updated with the new type symbol on successful binding.</param>
    /// <param name="typeMemberScopes">A container mapping type symbols to their corresponding member scopes. Updated with a new scope for the newly bound type.</param>
    /// <param name="db">A diagnostic bag used to report errors and warnings encountered during the binding process. Stores details about duplicate declarations and other issues.</param>
    /// <param name="typeNodesBySymbol">A mapping of type symbols to their corresponding type declaration nodes. Used to track relationships between symbols and the nodes they represent.</param>
    private static void BindDeclaration(
        Scope parentScope,
        ITypeDeclarationNode type,
        string parentFullName,
        string filePath,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes,
        DiagnosticBag db,
        Dictionary<NamedTypeSymbol, ITypeDeclarationNode> typeNodesBySymbol)
    {
        Logger.LogTrace($"Binding declaration '{type.Name}' in scope '{parentFullName}'");
        var fullName = $"{parentFullName}.{type.Name}";
        var typeSym = new NamedTypeSymbol(type.Name, fullName, NamedTypeKind.Class)
        {
            DeclarationSpan = type.Span with { FilePath = filePath }
        };
        
        typeNodesBySymbol[typeSym] = type;

        if (!parentScope.TryDeclare(typeSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Duplicate type symbol '{fullName}'", typeSym.DeclarationSpan);
            return;
        }
        
        symbolsByNode[type] = typeSym;
        typeMemberScopes[typeSym] = new Scope(parentScope);
    }

    /// <summary>
    /// Binds the members of a type declaration node by creating symbols, associating scopes,
    /// and resolving diagnostics. This method handles member declarations and ensures default
    /// constructors are generated when applicable.
    /// </summary>
    /// <param name="type">The type declaration node that contains members to be processed.</param>
    /// <param name="filePath">The file path associated with the type declaration, used for diagnostics and symbol spans.</param>
    /// <param name="symbolsByNode">A dictionary mapping AST nodes to their corresponding symbols.</param>
    /// <param name="typeMemberScopes">A dictionary containing the scope for members of each named type.</param>
    /// <param name="db">The diagnostic bag used to collect errors and warnings encountered during processing.</param>
    /// <param name="containingTypeByNode">A mapping of member nodes to their containing named type symbols.</param>
    private static void BindMember(
        ITypeDeclarationNode type,
        string filePath,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        Dictionary<NamedTypeSymbol, Scope> typeMemberScopes,
        DiagnosticBag db,
        Dictionary<IMemberNode, NamedTypeSymbol> containingTypeByNode)
    {
        if (type is not ClassDeclarationNode @class ||
            !symbolsByNode.TryGetValue(type, out var sym) ||
            sym is not NamedTypeSymbol typeSym)
            return;

        var memberScope = typeMemberScopes[typeSym];
        foreach (var member in @class.Members)
        {
            switch (member)
            {
                case PropertyDeclarationNode pdn:
                    Logger.LogTrace($"Binding property '{pdn.Name}' in type '{typeSym.FullName}'");
                    var propType = ResolveType(memberScope, pdn.Type, db);
                    var propSym = new PropertySymbol(pdn.Name, propType, typeSym, pdn.HasGetter, pdn.HasSetter)
                    {
                        DeclarationSpan = pdn.Span with { FilePath = filePath }
                    };
                    if (!memberScope.TryDeclare(propSym))
                        db.Error(ErrorCode.DuplicateSymbol, $"Property {pdn.Name} is already declared in {typeSym.FullName}");
                    symbolsByNode[pdn] = propSym;
                    break;
                case FieldDeclarationNode fdn:
                    Logger.LogTrace($"Binding field '{fdn.Name}' in type '{typeSym.FullName}'");
                    var fieldType = ResolveType(memberScope, fdn.Type, db);
                    var fieldSym = new FieldSymbol(fdn.Name, fieldType, typeSym)
                    {
                        DeclarationSpan = fdn.Span with { FilePath = filePath }
                    };
                    if (!memberScope.TryDeclare(fieldSym))
                        db.Error(ErrorCode.DuplicateSymbol, $"Field {fdn.Name} is already declared in {typeSym.FullName}");
                    symbolsByNode[fdn] = fieldSym;
                    break;
                case ConstructorDeclarationNode cdn:
                    Logger.LogTrace($"Binding constructor in type '{typeSym.FullName}'");
                    var ctorParams = BindParameters(memberScope, typeSym, filePath, cdn.Parameters, db);
                    var ctorSym = new ConstructorSymbol(typeSym, ctorParams)
                    {
                        DeclarationSpan = cdn.Span with { FilePath = filePath }
                    };
                    if (!memberScope.TryDeclare(ctorSym))
                        db.Error(ErrorCode.DuplicateSymbol, $"Constructor for type {typeSym.FullName} is already declared with the same parameters");
                    symbolsByNode[cdn] = ctorSym;
                    containingTypeByNode[cdn] = typeSym;
                    break;
                case MethodDeclarationNode mdn:
                    Logger.LogTrace($"Binding method '{mdn.Name}' in type '{typeSym.Name}'");
                    BindFunction(memberScope, mdn, filePath, symbolsByNode, typeSym, db);
                    containingTypeByNode[mdn] = typeSym;
                    break;
                default:
                    db.Error(ErrorCode.UnsupportedNode, $"Unexpected node type: {type.GetType().Name}");
                    break;
            }
        }

        if (@class.Members.OfType<ConstructorDeclarationNode>().Any()) return;
        var defaultCtor = new ConstructorSymbol(typeSym, BindParameters(memberScope, typeSym, filePath, [], db))
        {
            DeclarationSpan = type.Span with { FilePath = filePath }
        };
        memberScope.TryDeclare(defaultCtor);
    }

    /// <summary>
    /// Binds a method declaration to the specified scope, resolving its return type, parameters, and symbol,
    /// and associating the method with its corresponding type. This process ensures the function is correctly
    /// linked to the symbol table for later stages of compilation.
    /// </summary>
    /// <param name="scope">The scope in which the method is being declared and bound.</param>
    /// <param name="method">The method declaration node containing details about the method being bound.</param>
    /// <param name="filePath">The file path associated with the method declaration, used in diagnostics and error reporting.</param>
    /// <param name="symbolsByNode">A mapping of AST nodes to their respective symbols, updated with the method's symbol.</param>
    /// <param name="typeSym">The type symbol to which the method belongs, serving as the method's parent context.</param>
    /// <param name="db">The diagnostic bag used for collecting errors and warnings encountered during binding.</param>
    private static void BindFunction(
        Scope scope,
        MethodDeclarationNode method,
        string filePath,
        Dictionary<IAstNode, Symbol> symbolsByNode,
        NamedTypeSymbol typeSym,
        DiagnosticBag db)
    {
        var returnType = ResolveType(scope, method.ReturnType, db);
        Logger.LogTrace($"Return type of function '{method.Name}' is {returnType.Name}");
        var parameters = BindParameters(scope, typeSym, filePath, method.Parameters, db);
        var funcSym = new MethodSymbol(method.Name, returnType, parameters, typeSym)
        {
            DeclarationSpan = method.Span with { FilePath = filePath }
        };
        if (!scope.TryDeclare(funcSym))
        {
            db.Error(ErrorCode.DuplicateSymbol, $"Function {method.Name} is already declared in {typeSym.FullName}");
            return;
        }

        symbolsByNode[method] = funcSym;
    }

    /// <summary>
    /// Binds parameter declarations into a list of parameter symbols within the provided scope,
    /// associating parameters with their types, validating constraints, and generating diagnostics for invalid cases.
    /// </summary>
    /// <param name="lookupScope">The scope used to resolve types and ensure the validity of parameter symbols.</param>
    /// <param name="declaringType">The optional type declaring the parameters, used to add an implicit 'this' parameter if applicable.</param>
    /// <param name="filePath">The file path containing the parameter declarations, used for diagnostic reporting and context.</param>
    /// <param name="parameters">The list of parameter declarations to be processed and bound to symbols.</param>
    /// <param name="db">The diagnostic bag used to collect errors and warnings encountered during parameter binding.</param>
    /// <returns>A list of bound parameter symbols, including the implicit 'this' parameter if applicable.</returns>
    private static List<ParameterSymbol> BindParameters(
        Scope lookupScope,
        NamedTypeSymbol? declaringType,
        string filePath,
        IList<VParameter> parameters,
        DiagnosticBag db)
    {
        var list = new List<ParameterSymbol>(parameters.Count + 1);
        var ordinal = 0;
        // TODO: switch to static check instead of containing type check
        if (declaringType is not null)
        {
            list.Add(new ParameterSymbol("this", declaringType, ordinal++)
            {
                DeclarationSpan = declaringType.DeclarationSpan
            });
            Logger.LogTrace($"Adding 'this' parameter for type '{declaringType.Name}' at index 0");
        }

        if (parameters.Any(p => p.Name == "this"))
            db.Error(ErrorCode.DuplicateSymbol, "Parameter 'this' is not allowed in method parameters", declaringType?.DeclarationSpan!);
        foreach (var p in parameters)
        {
            if (p.Name == "this")
                continue;
            var pType = ResolveType(lookupScope, p.Type, db);
            Logger.LogTrace($"Binding parameter '{p.Name}' of type '{pType.Name}' at index {ordinal}");
            list.Add(new ParameterSymbol(p.Name, pType, ordinal++)
            {
                DeclarationSpan = p.Span with { FilePath = filePath }
            });
        }

        return list;
    }

    /// <summary>
    /// Declares built-in types in the provided scope, making them available for use
    /// during the binding phase. These types include common primitives and placeholders
    /// such as void, boolean, number, and error types.
    /// </summary>
    /// <param name="packageScope">The root scope where built-in types will be declared.</param>
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

    /// <summary>
    /// Retrieves an existing scope for the specified space or creates a new one if it does not already exist.
    /// This ensures that unique scopes are maintained for each space within the parent scope.
    /// </summary>
    /// <param name="parent">The parent scope in which the space is defined. Acts as the hierarchical context for the new or retrieved scope.</param>
    /// <param name="name">The name of the space for which the scope is to be created or retrieved.</param>
    /// <returns>A <see cref="Scope"/> object representing the scope for the specified space, either newly created or previously existing.</returns>
    private Scope GetOrCreateSpaceScope(Scope parent, string name)
    {
        Logger.LogTrace($"Creating scope for space '{name}'");
        var key = (parent, name);
        if (_spaceScopes.TryGetValue(key, out var existing))
        {
            Logger.LogTrace($"Reusing existing scope for space '{name}'");
            return existing;
        }

        var created = new Scope(parent);
        Logger.LogTrace($"Created new scope for space '{name}'");
        _spaceScopes[key] = created;
        return created;
    }

    /// <summary>
    /// Resolves a type symbol by searching within the provided scope and determining the corresponding
    /// type for the specified type name. Handles array type resolution and reports diagnostics for unknown types.
    /// </summary>
    /// <param name="lookupScope">The scope used to search for the type declaration.</param>
    /// <param name="typeName">The name of the type to resolve.</param>
    /// <param name="db">The diagnostic bag used to collect errors and warnings during the resolution process.</param>
    /// <returns>A <see cref="TypeSymbol"/> instance representing the resolved type. Returns an error type symbol if the type cannot be found.</returns>
    private static TypeSymbol ResolveType(Scope lookupScope, string typeName, DiagnosticBag db)
    {
        if (typeName.EndsWith("[]"))
        {
            var elementType = ResolveType(lookupScope, typeName[..^2], db);
            return BuiltInTypeSymbol.ArrayOf(elementType);
        }

        var type = lookupScope.Lookup(typeName).OfType<TypeSymbol>().FirstOrDefault();

        if (type is not null) return type;
        db.Error(ErrorCode.TypeNotFound, $"Unknown type '{typeName}'");
        return BuiltInTypeSymbol.Error;
    }
}
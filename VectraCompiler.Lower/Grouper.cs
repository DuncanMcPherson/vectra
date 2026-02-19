using VectraCompiler.AST;
using VectraCompiler.Bind.Bodies.Statements;
using VectraCompiler.Bind.Models.Symbols;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;
using VectraCompiler.Lower.Models;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Lower;

public static class Grouper
{
    public static Result<List<LoweredModule>> Run(
        LoweredResult loweredResult,
        IReadOnlyList<ModuleMetadata> modules,
        VectraAstPackage package)
    {
        var db = new DiagnosticBag();
        
        // Build a map from a source file path to the module name using the overall package
        var fileToModule = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in package.Modules)
        {
            foreach (var file in module.Files)
            {
                fileToModule[file.Tree.FilePath] = module.ModuleName;
            }
        }
        
        // group types by module
        var typesByModule = new Dictionary<string, List<NamedTypeSymbol>>(StringComparer.Ordinal);
        foreach (var type in loweredResult.AnalyzeResult.BindResult.Declarations.SymbolsByNode.Values
                     .OfType<NamedTypeSymbol>().Distinct())
        {
            var filePath = type.SourceFilePath;
            if (filePath is null || !fileToModule.TryGetValue(filePath, out var moduleName))
            {
                db.Error(ErrorCode.InternalError, $"Cannot resolve module for type '{type.FullName}'");
                continue;
            }

            if (!typesByModule.TryGetValue(moduleName, out var list))
                typesByModule[moduleName] = list = [];
            list.Add(type);
        }
        
        // Group lowered bodies by module via their containing type's source file
        var bodiesByModule = new Dictionary<string, Dictionary<CallableSymbol, BoundStatement>>(StringComparer.Ordinal);
        foreach (var (callable, body) in loweredResult.LoweredBodies)
        {
            var filePath = callable.SourceFilePath;

            if (filePath is null || !fileToModule.TryGetValue(filePath, out var moduleName))
            {
                db.Error(ErrorCode.InternalError, $"Cannot resolve module for callable body '{callable.Name}'");
                continue;
            }

            if (!bodiesByModule.TryGetValue(moduleName, out var dict))
                bodiesByModule[moduleName] = dict = [];
            dict[callable] = body;
        }

        if (db.HasErrors)
            return Result<List<LoweredModule>>.Fail(db);
        
        // Assemble lowered module per module
        var loweredModules = new Dictionary<string, LoweredModule>(StringComparer.Ordinal);
        foreach (var metadata in modules)
        {
            typesByModule.TryGetValue(metadata.Name, out var types);
            bodiesByModule.TryGetValue(metadata.Name, out var bodies);

            loweredModules[metadata.Name] = new LoweredModule
            {
                ModuleName = metadata.Name,
                ModuleType = metadata.Type,
                ModuleRoot = metadata.ModuleRoot,
                References = metadata.References,
                Types = types ?? [],
                LoweredBodies = bodies ?? []
            };
        }
        
        return Result<List<LoweredModule>>.Pass(loweredModules.Values.ToList(), db);
    }
}
using Spectre.Console;
using VectraCompiler.AST;
using VectraCompiler.AST.Models;
using VectraCompiler.AST.Models.Declarations.Interfaces;
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

                var packageScope = new Scope(null);
                var binder = new Binder();
                var declarations = new DeclarationBindResult
                {
                    PackageScope = packageScope,
                    SymbolsByNode = new Dictionary<IAstNode, Symbol>(),
                    TypeMemberScopes = new Dictionary<NamedTypeSymbol, Scope>(),
                    TypeNodesBySymbol = new Dictionary<NamedTypeSymbol, ITypeDeclarationNode>(),
                    ContainingTypeByNode = new Dictionary<IMemberNode, NamedTypeSymbol>(),
                    SpaceScopesByFullName = new Dictionary<(string ModuleName, string QualifiedName), Scope>(),
                    ImportedSpaces = new ImportedSpaceContext()
                };

                var typesTask = ctx.AddTask("Bind Symbols (Types and Spaces)");
                binder.BindTypes(package, packageScope, declarations, db);
                typesTask.Increment(1);
                typesTask.StopTask();

                ct.ThrowIfCancellationRequested();

                var membersTask = ctx.AddTask("Bind Symbols (Type members)");
                binder.BindMembers(declarations, db);
                membersTask.Increment(1);
                membersTask.StopTask();

                ct.ThrowIfCancellationRequested();
                var binderService = new BinderService(declarations, db);
                binderService.BindBodies(out var bodies, out var allocators);
                var bodyTask = ctx.AddTask("Bind Symbols (Method and Constructor Bodies)");
                bodyTask.Increment(1);
                bodyTask.StopTask();

                var bodyBindResult = new BodyBindResult
                {
                    BodiesByMember = bodies,
                    Declarations = declarations,
                    SlotAllocatorsByMember = allocators
                };
                var errorCount = db.Items.Count(x => x.Severity == Severity.Error);
                Logger.LogInfo($"Binding completed with {errorCount} errors.");
                var result = db.HasErrors
                    ? Result<BodyBindResult>.Fail(db)
                    : Result<BodyBindResult>.Pass(bodyBindResult, db);
                return Task.FromResult(result);
            });
    }
}
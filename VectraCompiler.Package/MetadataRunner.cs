using Spectre.Console;
using VectraCompiler.Core;
using VectraCompiler.Core.ConsoleExtensions;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public static class MetadataRunner
{
    public static async Task<Result<(SortResult, string)>> RunPhaseAsync(string? path = null, CancellationToken ct = default)
    {
        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new TaskDescriptionColumn {Alignment = Justify.Left}, new ProgressBarColumn(), new PercentageColumn(), new SpinnerColumn(),
                new ElapsedTimeMsColumn()).StartAsync(async ctx =>
            {
                using var _ = Logger.BeginPhase(CompilePhase.Metadata, "Starting phase: Metadata");

                var packageDataResult =
                    await GetModulesToCompileAsync(ctx, ct, path);
                return packageDataResult;
            });
    }
    
    private static async Task<Result<PackageMetadata>> CollectPackageDataAsync(ProgressContext ctx,
        CancellationToken ct, string? path = null)
    {
        var pkgTask = ctx.AddTask("[cyan]Find & parse package[/]", maxValue: 4);
        var packageDataResult = await PackageReader.Read(pkgTask, path, ct);
        pkgTask.Increment(1);
        pkgTask.StopTask();
        if (packageDataResult.Ok) return packageDataResult;
        foreach (var item in packageDataResult.Diagnostics.Items)
        {
            Logger.LogError($"{item.Code.ToCodeString()} - {item.Message}");
        }

        return packageDataResult;
    }

    private static async Task<Result<(SortResult, string)>> GetModulesToCompileAsync(ProgressContext ctx,
        CancellationToken ct,
        string? path = null)
    {
        var modules = await GetModuleMetadataAsync(ctx, ct, path);
        if (!modules.Ok) return Result<(SortResult, string)>.Fail(modules.Diagnostics);
        var graphTask = ctx.AddTask("[cyan]Build dependency graph[/]", maxValue: 4);
        var modGraphRes = DependencyGraphBuilder.TopoSort(modules.Value.Item1, graphTask);
        graphTask.Increment(1);
        if (modGraphRes.Ok && !modGraphRes.Value!.HasCycle)
            return Result<(SortResult, string)>.Pass((modGraphRes.Value, modules.Value.Item2), modGraphRes.Diagnostics);
        if (modGraphRes.Value != null)
        {
            Logger.LogError($"Circular dependency detected: {string.Join(", ", modGraphRes.Value!.CycleNodes)}");
        }
        else
        {
            foreach (var d in modGraphRes.Diagnostics.Items)
            {
                Logger.LogError($"{d.Code.ToCodeString()} - {d.Message}");
            }
        }

        return Result<(SortResult, string)>.Fail(modGraphRes.Diagnostics);
    }

    private static async Task<Result<(List<ModuleMetadata>, string)>> GetModuleMetadataAsync(ProgressContext ctx,
        CancellationToken ct, string? path = null)
    {
        var packageDataResult = await CollectPackageDataAsync(ctx, ct, path);
        if (!packageDataResult.Ok) return Result<(List<ModuleMetadata>, string)>.Fail(packageDataResult.Diagnostics);

        List<ModuleMetadata> moduleData = [];
        var foundError = false;

        var packageMetadata = packageDataResult.Value!;
        var modTasks = packageMetadata.Modules.Select(m => ctx.AddTask($"[cyan]{m.Name} metadata[/]", false, 3))
            .ToList();
        var modules = packageMetadata.Modules;
        for (var i = 0; i < modules.Count; i++)
        {
            var task = modTasks[i];
            var mod = modules[i];
            task.StartTask();
            var modData = await ModuleReader.Read(task, mod, ct);
            if (!modData.Ok)
            {
                foundError = true;
                foreach (var item in modData.Diagnostics.Items)
                {
                    Logger.LogError($"{item.Code.ToCodeString()} - {item.Message}");
                }
            }
            else
            {
                moduleData.Add(modData.Value!);
            }

            task.Increment(1);
            task.StopTask();
        }

        return foundError
            ? Result<(List<ModuleMetadata>, string)>.Fail(packageDataResult.Diagnostics)
            : Result<(List<ModuleMetadata>, string)>.Pass((moduleData, packageMetadata.Name),
                packageDataResult.Diagnostics);
    }
}
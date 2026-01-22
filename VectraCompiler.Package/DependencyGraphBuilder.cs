using Spectre.Console;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public sealed record SortResult(
    IReadOnlyList<ModuleMetadata> Order,
    IReadOnlyList<string> CycleNodes)
{
    public bool HasCycle => CycleNodes.Count > 0;
}

public static class DependencyGraphBuilder
{
    public static Result<SortResult> TopoSort(IReadOnlyCollection<ModuleMetadata> modules, ProgressTask task)
    {
        var db = new DiagnosticBag();
        var byName = modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var dependsOn = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var m in modules)
        {
            dependsOn[m.Name] = new HashSet<string>(StringComparer.Ordinal);
            dependents[m.Name] = new HashSet<string>(StringComparer.Ordinal);
        }
        
        task.Increment(1);

        var errors = new List<string>();

        foreach (var m in modules)
        {
            foreach (var dep in m.References)
            {
                if (!byName.ContainsKey(dep))
                {
                    errors.Add($"Unknown module referenced by {m.Name}: {dep}");
                    continue;
                }

                dependsOn[m.Name].Add(dep);
                dependents[dep].Add(m.Name);
            }
        }
        task.Increment(1);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                db.Error(ErrorCode.ModuleNotFound, error);
            return Result<SortResult>.Fail(db);
        }

        var indegree = dependsOn.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count, StringComparer.Ordinal);
        var queue = new Queue<string>(indegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var order = new List<ModuleMetadata>(modules.Count);

        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            order.Add(byName[n]);

            foreach (var dep in dependents[n])
            {
                indegree[dep]--;
                if (indegree[dep] == 0)
                {
                    queue.Enqueue(dep);
                }
            }
        }
        
        task.Increment(1);

        if (order.Count != modules.Count)
        {
            var cycleNodes = indegree
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            return Result<SortResult>.Pass(new SortResult(order, cycleNodes), db);
        }

        return Result<SortResult>.Pass(new SortResult(order, []), db);
    }
}
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
    public static SortResult TopoSort(IReadOnlyCollection<ModuleMetadata> modules)
    {
        var byName = modules.ToDictionary(m => m.Name, StringComparer.Ordinal);
        var dependsOn = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var dependents = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var m in modules)
        {
            dependsOn[m.Name] = new HashSet<string>(StringComparer.Ordinal);
            dependents[m.Name] = new HashSet<string>(StringComparer.Ordinal);
        }

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

        if (errors.Count > 0)
        {
            throw new ModuleDependencyException(string.Join(Environment.NewLine, errors));
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

        if (order.Count != modules.Count)
        {
            var cycleNodes = indegree
                .Where(kvp => kvp.Value > 0)
                .Select(kvp => kvp.Key)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            return new SortResult(order, cycleNodes);
        }

        return new SortResult(order, []);
    }
}

public class ModuleDependencyException : Exception
{
    public ModuleDependencyException(string message) : base(message)
    {
    }
}
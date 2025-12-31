using VectraCompiler.Package.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace VectraCompiler.Package;

public static class SourceFileDiscoverer
{
    public static IReadOnlyList<string> Discover(ModuleMetadata module)
    {
        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        foreach (var pattern in module.Sources)
        {
            matcher.AddInclude(pattern);
        }

        var directoryInfo = new DirectoryInfoWrapper(
            new DirectoryInfo(Path.GetPathRoot(module.Sources[0])!));
        var pathRoot = Path.GetPathRoot(module.Sources[0])!;

        var matches = matcher.Execute(directoryInfo);
        var files = matches.Files
            .Select(f => Path.GetFullPath(Path.Combine(pathRoot, f.Path)))
            // .Select(f => f.Path)
            .Where(p => p.EndsWith(".vec", StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
        if (files.Count == 0)
        {
            Console.WriteLine($"WARNING: Pattern did not match any files.\nModule: {module.Name}");
        }

        return files;
    }
}
using VectraCompiler.Package.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using VectraCompiler.Core.Logging;

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
            new DirectoryInfo(module.ModuleRoot));
        var pathRoot = module.ModuleRoot;

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
            Logger.LogWarning(CompilePhase.Metadata, $"Pattern did not match any files.\n\tModule: {module.Name}");
        }

        return files;
    }
}
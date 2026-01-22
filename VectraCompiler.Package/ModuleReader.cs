using Spectre.Console;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public static class ModuleReader
{
    public static async Task<Result<ModuleMetadata>> Read(ProgressTask task, ModuleLocation locationData, CancellationToken ct)
    {
        Logger.LogInfo($"Resolving metadata for module: {locationData.Name}");
        var db = new DiagnosticBag();
        // We already verified that the module file exists and normalized the path
        var lines = (await File.ReadAllLinesAsync(locationData.Path, ct))
            .Select(l => l.Trim())
            .Where(l => !l.IsNullOrEmpty())
            .ToList();
        task.Increment(1);
        var headerParts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (headerParts is not ["module", _])
        {
            return Result<ModuleMetadata>.Fail(db.Error(ErrorCode.ModuleFormatInvalid, $"Invalid module header: {lines[0]}"));
        }

        var moduleName = headerParts[1];
        var moduleType = ModuleType.Executable;
        var references = new List<string>();
        var dependencies = new List<string>();
        var sources = new List<string>();
        var seenSections = new HashSet<string>();

        task.Increment(1);
        var index = 1;
        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.EndsWith('{'))
            {
                var sectionName = line[..^1].Trim();
                index++;

                var sectionLines = new List<string>();

                while (index < lines.Count && lines[index] != "}")
                {
                    sectionLines.Add(lines[index]);
                    index++;
                }

                if (index == lines.Count)
                    return Result<ModuleMetadata>.Fail(db.Error(ErrorCode.ModuleFormatInvalid, $"Unclosed section: {sectionName}"));
                index++;
                if (!seenSections.Add(sectionName))
                    return Result<ModuleMetadata>.Fail(db.Error(ErrorCode.ModuleFormatInvalid, $"Duplicate section '{sectionName}'"));

                switch (sectionName)
                {
                    case "metadata":
                        ProcessModuleMetadata(sectionLines, out moduleType, db);
                        break;
                    case "dependencies":
                        ProcessSection(sectionLines, out dependencies);
                        break;
                    case "references":
                        ProcessSection(sectionLines, out references);
                        break;
                    case "sources":
                        ProcessSources(sectionLines, locationData.Path, out sources);
                        break;
                    default:
                        return Result<ModuleMetadata>.Fail(db.Error(ErrorCode.ModuleFormatInvalid, $"Unknown section: {sectionName}"));
                }
            }
            else
            {
                return Result<ModuleMetadata>.Fail(db.Error(ErrorCode.ModuleFormatInvalid, $"Unexpected line: {line}"));
            }
        }
        
        if (db.HasErrors) return Result<ModuleMetadata>.Fail(db);

        var moduleRoot = Directory.GetParent(locationData.Path)!.FullName;

        return Result<ModuleMetadata>.Pass(new ModuleMetadata
        {
            Dependencies = dependencies,
            Name = moduleName,
            References = references,
            Type = moduleType,
            Sources = sources,
            ModuleRoot = moduleRoot
        }, db);
    }

    private static void ProcessModuleMetadata(List<string> lines, out ModuleType moduleType, DiagnosticBag db)
    {
        moduleType = default;
        foreach (var parts in from line in lines where line.StartsWith("type") select line.Split(' '))
        {
            if (parts.Length != 2)
            {
                db.Error(ErrorCode.ModuleFormatInvalid, "Invalid module type definition");
                return;
            }

            moduleType = parts[1].ToModuleType();
            Logger.LogTrace($"Module is: {moduleType}");
            return;
        }

        moduleType = ModuleType.Executable;
        Logger.LogTrace($"Module is: {moduleType}");
    }

    private static void ProcessSection(List<string> lines, out List<string> sectionData)
    {
        // for now, we are just defining it as a raw name
        sectionData = new List<string>(lines);
    }

    private static void ProcessSources(List<string> lines, string modulePath, out List<string> sourcesData)
    {
        sourcesData = [];
        foreach (var line in lines)
        {
            
            sourcesData.Add(line);

        }
    }
}
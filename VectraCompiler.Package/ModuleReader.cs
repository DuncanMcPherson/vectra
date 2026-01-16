using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public static class ModuleReader
{
    public static ModuleMetadata Read(ModuleLocation locationData)
    {
        Console.WriteLine($"Resolving metadata for module: {locationData.Name}");
        // We already verified that the module file exists and normalized the path
        var lines = File.ReadLines(locationData.Path)
            .Select(l => l.Trim())
            .Where(l => !l.IsNullOrEmpty())
            .ToList();
        var headerParts = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (headerParts is not ["module", _])
        {
            throw new Exception(
                "Invalid module header. Module header must include 'module' followed by a module name with no spaces");
        }

        var moduleName = headerParts[1];
        var moduleType = ModuleType.Executable;
        var references = new List<string>();
        var dependencies = new List<string>();
        var sources = new List<string>();
        var seenSections = new HashSet<string>();

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
                    throw new Exception($"Unclosed section: {sectionName}");
                index++;
                if (!seenSections.Add(sectionName))
                    throw new Exception($"Duplicate section '{sectionName}'");

                switch (sectionName)
                {
                    case "metadata":
                        ProcessModuleMetadata(sectionLines, out moduleType);
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
                        throw new Exception($"Unknown section: {sectionName}");
                }
            }
            else
            {
                throw new Exception($"Unexpected line: {line}");
            }
        }

        var moduleRoot = Directory.GetParent(locationData.Path)!.FullName;

        return new ModuleMetadata
        {
            Dependencies = dependencies,
            Name = moduleName,
            References = references,
            Type = moduleType,
            Sources = sources,
            ModuleRoot = moduleRoot
        };
    }

    private static void ProcessModuleMetadata(List<string> lines, out ModuleType moduleType)
    {
        foreach (var parts in from line in lines where line.StartsWith("type") select line.Split(' '))
        {
            if (parts.Length != 2)
            {
                throw new Exception("Invalid module type definition");
            }

            moduleType = parts[1].ToModuleType();
            return;
        }

        moduleType = ModuleType.Executable;
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
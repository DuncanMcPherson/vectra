using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public static class PackageReader
{
    public static PackageMetadata Read(string? path)
    {
        path = ResolvePackagePath(path);
        var lines = File.ReadLines(path)
            .Select(l => l.Trim())
            .Where(l => !l.IsNullOrEmpty())
            .ToList();
        var pkgLines = lines.Where(s => s.StartsWith("package")).ToList();
        if (pkgLines.Count != 1)
        {
            throw new Exception($"Invalid vpkg format. Too {(pkgLines.Count == 0 ? "few" : "many")} 'package' lines");
        }

        var pkgLine = pkgLines[0];
        var pkgParts = pkgLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pkgParts is not ["package", _])
            throw new Exception($"Invalid vpkg format. Invalid package line '{pkgLine}'");
        var pkgName = pkgParts[1];
        if (pkgName.IsNullOrEmpty())
            throw new Exception("Package name not found.");
        var modLines = lines.Where(s => s.StartsWith("module")).ToList();
        if (modLines.Count == 0)
        {
            throw new Exception("No modules found.");
        }

        var modList = new List<ModuleLocation>();
        var errorsList = new List<string>();
        var pkgDirectory = Path.GetDirectoryName(path)!;

        foreach (var lineParts in modLines.Select(modLine => modLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        {
            // first entry should always be "module"
            // second entry should be the module name
            // third entry should be the path to the vmod file. this path can be absolute or relative
            if (lineParts.Length != 3)
            {
                errorsList.Add("Invalid vpkg format. Module definitions must include 3 parts.");
                continue;
            }

            var modName = lineParts[1];
            var modPath = lineParts[2];
            if (!modPath.EndsWith(".vmod"))
            {
                errorsList.Add($"Module path is not a vmod file: {modPath}");
                continue;
            }
            
            if (!Path.IsPathFullyQualified(modPath))
            {
                modPath = Path.Join(pkgDirectory, modPath);
            }

            if (!File.Exists(modPath))
            {
                errorsList.Add($"Module path not found: {modPath}");
                continue;
            }
            modList.Add(new ModuleLocation
            {
                Name = modName,
                Path = modPath
            });
        }

        if (errorsList.Count != 0)
        {
            throw new Exception(string.Join('\n', errorsList));
        }

        return new PackageMetadata
        {
            Name = pkgName,
            Modules = modList
        };
    }

    private static string ResolvePackagePath(string? path)
    {
        var baseDir = path.IsNullOrEmpty()
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(path!);
        if (File.Exists(baseDir))
        {
            return !baseDir.EndsWith(".vpkg") ? throw new Exception("File is not a vpkg file.") : baseDir;
        }
        
        if (!Directory.Exists(baseDir))
            throw new Exception("Directory not found.");

        var files = Directory.EnumerateFiles(baseDir, "*.vpkg", SearchOption.TopDirectoryOnly)
            .ToList();
        return files.Count switch
        {
            0 => throw new Exception("No .vpkg found"),
            > 1 => throw new Exception("Multiple .vpkg files found. Please specify which you would like to use"),
            _ => files[0]
        };
    }
}
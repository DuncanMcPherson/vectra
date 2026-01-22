using Spectre.Console;
using VectraCompiler.Core;
using VectraCompiler.Core.Errors;
using VectraCompiler.Core.Logging;
using VectraCompiler.Package.Models;

namespace VectraCompiler.Package;

public static class PackageReader
{
    public static async Task<Result<PackageMetadata>> Read(ProgressTask task, string? path, CancellationToken ct)
    {
        var dBag = new DiagnosticBag();
        var res = ResolvePackagePath(path, dBag);
        if (!res.Ok)
        {
            return Result<PackageMetadata>.Fail(res.Diagnostics);
        }

        path = res.Value!;
        var lines = (await File.ReadAllTextAsync(path, ct))
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !l.IsNullOrEmpty())
            .ToList();
        task.Increment(1);
        var pkgLines = lines.Where(s => s.StartsWith("package")).ToList();
        if (pkgLines.Count != 1)
        {
            return Result<PackageMetadata>.Fail(dBag.Error(ErrorCode.PackageFormatInvalid, "Invalid vpkg format. Only one package line allowed."));
        }

        var pkgLine = pkgLines[0];
        var pkgParts = pkgLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pkgParts is not ["package", _])
            return Result<PackageMetadata>.Fail(dBag.Error(ErrorCode.PackageFormatInvalid, $"Invalid package line '{pkgLine}'"));
        var pkgName = pkgParts[1];
        if (pkgName.IsNullOrEmpty())
            return Result<PackageMetadata>.Fail(dBag.Error(ErrorCode.PackageFormatInvalid, "Package name not found."));
        task.Increment(1);
        var modLines = lines.Where(s => s.StartsWith("module")).ToList();
        if (modLines.Count == 0)
        {
            return Result<PackageMetadata>.Fail(dBag.Error(ErrorCode.PackageFormatInvalid, "No modules found."));
        }

        var modList = new List<ModuleLocation>();
        var errorsList = new List<string>();
        var pkgDirectory = Path.GetDirectoryName(path)!;

        foreach (var lineParts in modLines.Select(modLine => modLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
        {
            // the first entry should always be "module"
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
        task.Increment(1);

        if (errorsList.Count != 0)
        {
           errorsList.ForEach(e => dBag.Error(ErrorCode.ModuleNotFound, e));
           return Result<PackageMetadata>.Fail(dBag);
        }

        foreach (var mod in modList)
        {
            Logger.LogTrace($"Found module: {mod.Name}");
        }

        return new Result<PackageMetadata>(new PackageMetadata
        {
            Name = pkgName,
            Modules = modList
        }, dBag);
    }

    private static Result<string> ResolvePackagePath(string? path, DiagnosticBag db)
    {
        var baseDir = path.IsNullOrEmpty()
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(path!);
        if (File.Exists(baseDir))
        {
            if (baseDir.EndsWith(".vpkg")) return new Result<string>(baseDir, db);
            db.Error(ErrorCode.PackageFormatInvalid, "File is not a vpkg file.");
            return new Result<string>(null, db);

        }

        if (!Directory.Exists(baseDir))
        {
            db.Error(ErrorCode.FileNotFound, $"Directory '{baseDir}' not found.");
            return new Result<string>(null, db);
        }

        var files = Directory.EnumerateFiles(baseDir, "*.vpkg", SearchOption.TopDirectoryOnly)
            .ToList();
        return files.Count switch
        {
            0 => Result<string>.Fail(db.Error(ErrorCode.FileNotFound, $"No .vpkg files found in '{baseDir}'.")),
            > 1 => Result<string>.Fail(db.Error(ErrorCode.FileNotFound, $"Multiple .vpkg files found in '{baseDir}'.")),
            _ => Result<string>.Pass(files[0], db)
        };
    }
}
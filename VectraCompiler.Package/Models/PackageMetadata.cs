namespace VectraCompiler.Package.Models;

public class PackageMetadata
{
    public required string Name { get; init; }
    public required IList<ModuleLocation> Modules { get; init; } = [];

    public override string ToString()
    {
        return $"Package: {Name}\n\nModules: [\n{string.Join(",\n", Modules)}\n]";
    }
}

public readonly struct ModuleLocation
{
    public string Name { get; init; }
    public string Path { get; init; }

    public override string ToString()
    {
        return $"Name: {Name}, Path: {Path}";
    }
}
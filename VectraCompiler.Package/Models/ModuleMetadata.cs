namespace VectraCompiler.Package.Models;

public sealed class ModuleMetadata
{
    public required string Name { get; init; }
    public required ModuleType Type { get; init; }

    public IReadOnlyList<string> Dependencies { get; init; } = [];
    public IReadOnlyList<string> References { get; init; } = [];
    public IReadOnlyList<string> Sources { get; init; } = [];

    public override string ToString()
    {
        return
            $"Module: {Name}\nType: {Type}\nDependency Count: {Dependencies.Count}\nReference Count: {References.Count}\nSources: [\n{string.Join("\n", Sources)}\n]";
    }
}

public enum ModuleType
{
    Library,
    Executable
}

public static class ModuleTypeExtensions
{
    public static ModuleType ToModuleType(this string value)
    {
        return value switch
        {
            "executable" => ModuleType.Executable,
            "library" => ModuleType.Library,
            _ => throw new Exception($"Invalid module type: {value}")
        };
    }
}
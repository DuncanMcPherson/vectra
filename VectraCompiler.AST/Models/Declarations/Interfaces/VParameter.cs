namespace VectraCompiler.AST.Models.Declarations.Interfaces;

public class VParameter(string name, string type)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
}
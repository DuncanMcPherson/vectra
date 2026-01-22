namespace VectraCompiler.AST;

public class VectraAstPackage
{
    public string Name { get; init; }
    public List<VectraAstModule> Modules { get; } = new();

    public void AddModule(VectraAstModule module)
    {
        Modules.Add(module);
    }
}
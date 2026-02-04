namespace VectraCompiler.AST;

public class VectraAstModule
{
    public required string ModuleName { get; init; }
    public List<ParseResult> Files { get; } = new();
}
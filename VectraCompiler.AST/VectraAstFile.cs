using VectraCompiler.AST.Models.Declarations;

namespace VectraCompiler.AST;

// TODO: Add FileId, and Version
public sealed record VectraAstFile
{
    public string FileName => Path.GetFileName(FilePath);
    public required List<EnterDirectiveNode> EnterDirectives { get; init; }
    public required string FilePath { get; init; }
    public required SpaceDeclarationNode Space { get; init; }
}
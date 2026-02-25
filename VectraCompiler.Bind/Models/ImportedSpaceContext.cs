namespace VectraCompiler.Bind.Models;

/// <summary>
/// Holds the set of spaces imported into a file via enter directives
/// Keyed by the file path
/// </summary>
public sealed class ImportedSpaceContext
{
    private readonly Dictionary<string, List<Scope>> _fileImports = new();

    public void AddImport(string filePath, Scope spaceScope)
    {
        if (!_fileImports.TryGetValue(filePath, out var list))
        {
            list = [];
            _fileImports[filePath] = list;
        }
        list.Add(spaceScope);
    }
    
    public IReadOnlyList<Scope> GetImports(string filePath)
        => _fileImports.TryGetValue(filePath, out var list) ? list : [];
}
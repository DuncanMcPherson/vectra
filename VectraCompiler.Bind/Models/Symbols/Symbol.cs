using VectraCompiler.Core;

namespace VectraCompiler.Bind.Models.Symbols;

public enum SymbolKind
{
    Type,
    Function,
    Parameter,
    Local,
    Constructor,
    Property,
    Field
}

public abstract class Symbol
{
    public SymbolKind Kind { get; }
    public string Name { get; }
    public SourceSpan? DeclarationSpan { get; init; }
    public string? SourceFilePath => DeclarationSpan?.FilePath;
    
    protected Symbol(SymbolKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }
    
    public override string ToString() => $"{Kind} {Name}";
}
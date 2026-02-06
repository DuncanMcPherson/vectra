namespace VectraCompiler.Bind.Models;

public enum SymbolKind
{
    Package,
    Module,
    Type,
    Function,
    Parameter,
    Local
}

public abstract class Symbol
{
    public SymbolKind Kind { get; }
    public string Name { get; }
    
    protected Symbol(SymbolKind kind, string name)
    {
        Kind = kind;
        Name = name;
    }
    
    public override string ToString() => $"{Kind} {Name}";
}
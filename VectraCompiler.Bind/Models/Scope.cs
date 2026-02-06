namespace VectraCompiler.Bind.Models;

public sealed class Scope
{
    private readonly Dictionary<string, List<Symbol>> _symbols = new(StringComparer.Ordinal);
    public Scope? Parent { get; }
    
    public Scope(Scope? parent) => Parent = parent;

    public bool TryDeclare(Symbol symbol)
    {
        if (!_symbols.TryGetValue(symbol.Name, out var list))
            _symbols[symbol.Name] = list = [];
        if (symbol is FunctionSymbol)
        {
            list.Add(symbol);
            return true;
        }
        if (list.Count > 0) return false;
        
        list.Add(symbol);
        return true;
    }

    public IReadOnlyList<Symbol> Lookup(string name)
    {
        for (var s = this; s != null; s = s.Parent)
            if (s._symbols.TryGetValue(name, out var list))
                return list;
        return [];
    }
}
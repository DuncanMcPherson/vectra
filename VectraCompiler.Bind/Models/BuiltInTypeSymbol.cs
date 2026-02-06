namespace VectraCompiler.Bind.Models;

public sealed class BuiltInTypeSymbol : TypeSymbol
{
    private BuiltInTypeSymbol(string name) : base(name)
    {
    }
    
    public static readonly BuiltInTypeSymbol Void = new("void");
    public static readonly BuiltInTypeSymbol Bool = new("bool");
    public static readonly BuiltInTypeSymbol Number = new("number");
    public static readonly BuiltInTypeSymbol String = new("string");

    public static readonly BuiltInTypeSymbol Error = new("<error>");

}
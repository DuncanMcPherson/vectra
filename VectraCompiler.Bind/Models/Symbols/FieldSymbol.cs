namespace VectraCompiler.Bind.Models.Symbols;

public sealed class FieldSymbol(string name, TypeSymbol type) : VariableSymbol(SymbolKind.Field, name, type);
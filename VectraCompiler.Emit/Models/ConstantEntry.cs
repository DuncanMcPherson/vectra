using System.Globalization;

namespace VectraCompiler.Emit.Models;

public sealed class ConstantEntry
{
    public ushort Index { get; }
    public ConstantKind Kind { get; }
    public string Name { get; }
    public double? NumericValue { get; }

    private ConstantEntry(ushort index, ConstantKind kind, string name, double? numericValue = null)
    {
        Index = index;
        Kind = kind;
        Name = name;
        NumericValue = numericValue;
    }

    internal static ConstantEntry ForSymbol(ushort index, ConstantKind kind, string name)
        => new(index, kind, name);

    internal static ConstantEntry ForString(ushort index, string value)
        => new(index, ConstantKind.String, value);

    internal static ConstantEntry ForNumber(ushort index, double value)
        => new(index, ConstantKind.Number, value.ToString(CultureInfo.InvariantCulture), value);
}
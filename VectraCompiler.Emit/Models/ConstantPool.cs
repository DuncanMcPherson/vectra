using VectraCompiler.Bind.Models.Symbols;

namespace VectraCompiler.Emit.Models;

public sealed class ConstantPool
{
    private readonly List<ConstantEntry> _entries = [];
    private readonly Dictionary<string, ushort> _symbolIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ushort> _stringIndex = new(StringComparer.Ordinal);
    private readonly Dictionary<double, ushort> _numberIndex = [];

    public IReadOnlyList<ConstantEntry> Entries => _entries;

    public ushort AddType(NamedTypeSymbol type)
        => GetOrAdd(_symbolIndex, type.FullName, () =>
            ConstantEntry.ForSymbol((ushort)_entries.Count, ConstantKind.Type, type.FullName));

    public ushort AddConstructor(ConstructorSymbol ctor)
        => GetOrAdd(_symbolIndex, CtorKey(ctor), () =>
            ConstantEntry.ForSymbol((ushort)_entries.Count, ConstantKind.Constructor, CtorKey(ctor)));

    public ushort AddMethod(MethodSymbol method)
        => GetOrAdd(_symbolIndex, MethodKey(method), () =>
            ConstantEntry.ForSymbol((ushort)_entries.Count, ConstantKind.Method, MethodKey(method)));

    public ushort AddField(FieldSymbol field)
        => GetOrAdd(_symbolIndex, MemberKey(field), () =>
            ConstantEntry.ForSymbol((ushort)_entries.Count, ConstantKind.Field, MemberKey(field)));

    public ushort AddProperty(PropertySymbol property)
        => GetOrAdd(_symbolIndex, MemberKey(property), () =>
            ConstantEntry.ForSymbol((ushort)_entries.Count, ConstantKind.Property, MemberKey(property)));

    public ushort AddString(string value)
        => GetOrAdd(_stringIndex, value, () =>
            ConstantEntry.ForString((ushort)_entries.Count, value));

    public ushort AddNumber(double value)
    {
        if (_numberIndex.TryGetValue(value, out var existing))
            return existing;
        var entry = ConstantEntry.ForNumber((ushort)_entries.Count, value);
        var index = (ushort)_entries.Count;
        _entries.Add(entry);
        _numberIndex[value] = index;
        return index;
    }

    private ushort GetOrAdd(Dictionary<string, ushort> index, string key, Func<ConstantEntry> factory)
    {
        if (index.TryGetValue(key, out var existing))
            return existing;
        var entry = factory();
        var idx = (ushort)_entries.Count;
        _entries.Add(entry);
        index[key] = idx;
        return idx;
    }

    private static string CtorKey(ConstructorSymbol ctor)
        => $"{ctor.SourceFilePath}::.ctor({string.Join(",", ctor.Parameters.Skip(1).Select(p => p.Type.Name))})";

    private static string MethodKey(MethodSymbol method)
        => $"{method.SourceFilePath}::{method.Name}({string.Join(",", method.Parameters.Skip(1).Select(p => p.Type.Name))})";

    private static string MemberKey(Symbol member)
        => $"{member.SourceFilePath}::{member.Name}";
}
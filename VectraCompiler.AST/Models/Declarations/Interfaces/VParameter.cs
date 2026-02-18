using VectraCompiler.Core;

namespace VectraCompiler.AST.Models.Declarations.Interfaces;

public class VParameter(string name, string type, SourceSpan span)
{
    public string Name { get; } = name;
    public string Type { get; } = type;
    public SourceSpan Span { get; } = span;

    public override string ToString()
    {
        return $"{Type} {Name}";
    }
}
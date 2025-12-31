namespace VectraCompiler.AST;

public static class Extensions
{
    public static void AddRange<T>(this IList<T>? collection, IEnumerable<T>? items)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
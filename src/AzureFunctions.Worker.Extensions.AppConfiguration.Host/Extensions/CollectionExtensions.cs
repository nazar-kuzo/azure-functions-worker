namespace System.Collections.Generic;

internal static class CollectionExtensions
{
    public static void Deconstruct<T>(
        this IEnumerable<T> collection,
        out T? first,
        out T? second)
    {
        first = collection.ElementAtOrDefault(0);
        second = collection.ElementAtOrDefault(1);
    }
}

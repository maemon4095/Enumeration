using System.Collections.Generic;

namespace Enumeration.Generator;
static class Extensions
{
    public static IEnumerable<(T0, T1)> Product<T0, T1>(this IEnumerable<T0> src, IEnumerable<T1> seq)
    {
        return src.SelectMany(i => seq.Select(j => (i, j)));
    }
}
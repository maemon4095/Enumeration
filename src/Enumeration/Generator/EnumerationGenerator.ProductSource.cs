using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorSupplement;
using System.Collections.Immutable;

namespace Enumeration.Generator;
partial class EnumerationGenerator
{
    private void ProductSource(SourceProductionContext context, Bundle bundle)
    {
        var writer = new IndentedWriter(IndentString);
    }
}
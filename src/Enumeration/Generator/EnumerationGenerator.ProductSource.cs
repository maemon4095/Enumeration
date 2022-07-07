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
        if (bundle.Symbol.IsValueType)
        {
            ProductStruct(writer, in bundle);
        }
        else
        {
            ProductClass(writer, in bundle);
        }
    }

    static void ProductClass(IndentedWriter writer, in Bundle bundle)
    {
        using (writer.DeclarationScope(bundle.Symbol))
        {

        }
    }

    static void ProductStruct(IndentedWriter writer, in Bundle bundle)
    {

    }
}
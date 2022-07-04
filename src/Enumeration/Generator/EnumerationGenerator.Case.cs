using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorSupplement;
using System.Collections.Immutable;

namespace Enumeration.Generator;
partial class EnumerationGenerator
{
    static IEnumerable<IEnumerable<Case>> CreateCases(ISymbol symbol, INamedTypeSymbol attribute)
    {
        symbol.GetAttributes()
              .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute))
              .Select(a => (Data: a, Syntax: (a.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax)!))
              .Where(pair => pair.Syntax is not null)
              .GroupBy(pair =>
              {
                  var (a, s) = pair;
                  return s.Parent;
              },
              pair =>
              {
                  var (a, s) = pair;
                  return CreateCase(a);
              });
    }

    static Case CreateCase(AttributeData attr)
    {
    }
}
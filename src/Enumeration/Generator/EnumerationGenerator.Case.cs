using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
              .Select(pair => (pair.Syntax.Parent, Case: CreateCase(pair.Data)))
              .Where(pair => pair.Case.HasValue)
              .GroupBy(pair => pair.Parent, pair => pair.Case!.Value);
    }

    static Case? CreateCase(AttributeData attr)
    {
        if (attr.ConstructorArguments[0].Value is not string identifier) return null;
        var parts = attr.ConstructorArguments.Skip(1)
                                             .Select(c => c.Value)
                                             .OfType<INamedTypeSymbol>()
                                             .Select((s, i) => new CasePart { Identifier = $"arg{i}", Kind = RefKind.In, Type = s })
                                             .ToImmutableArray();
        return new Case { Identifier = identifier, IsPartial = false, Parts = parts };

    }
}
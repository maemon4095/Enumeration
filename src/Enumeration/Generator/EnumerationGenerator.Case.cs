using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Enumeration.Generator;
partial class EnumerationGenerator
{
    static IEnumerable<Case>? CreateCases(INamedTypeSymbol symbol, INamedTypeSymbol attribute, ref PreprocessContext context)
    {
        var attrCases = symbol.GetAttributes()
                              .Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute))
                              .Select(a => CreateCase(a))
                              .Where(c => c.HasValue)
                              .Select(c => c!.Value);

        var memberCases = symbol.GetMembers()
                                .OfType<IMethodSymbol>()
                                .Select(m => CreateCase(m))
                                .Where(c => c.HasValue)
                                .Select(c => c!.Value);


        var builder = ImmutableHashSet.CreateBuilder<Case>();
        foreach (var c in attrCases.Concat(memberCases))
        {
            if (builder.Add(c)) continue;
            var diagnostic = Diagnostics.CaseDuplicationError(c.Location, c.Identifier);
            context.AddDiagnostic(diagnostic);
            return null;
        }
        return builder.ToImmutable();
    }

    static Case? CreateCase(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length <= 0) return null;
        if (attr.ConstructorArguments[0].Value is not string identifier) return null;
        var parts = attr.ConstructorArguments.Skip(1)
                                             .Select(c => c.Value)
                                             .OfType<INamedTypeSymbol>()
                                             .Select((s, i) => new CasePart { Identifier = $"arg{i}", Kind = RefKind.In, Type = s })
                                             .ToImmutableArray();
        return new Case { Identifier = identifier, Method = null, Parts = parts, Location = attr.ApplicationSyntaxReference?.GetSyntax().GetLocation()  };
    }

    static Case? CreateCase(IMethodSymbol method)
    {
        if (method is null ||
            !method.IsStatic ||
            method.DeclaredAccessibility != Accessibility.Public ||
            !method.CanBeReferencedByName ||
            !method.IsPartialDefinition ||
            method.MethodKind != MethodKind.Ordinary)
        {
            return null;
        }

        var parts = method.Parameters.Select(p => new CasePart
        {
            Identifier = p.Name,
            Kind = p.RefKind,
            Type = (p.Type as INamedTypeSymbol)!
        }).ToImmutableArray();

        return new Case { Identifier = method.Name, Method = method, Parts = parts };
    }


    readonly struct Case
    {
        public string Identifier { get; init; }
        public IMethodSymbol? Method { get; init; }
        public ImmutableArray<CasePart> Parts { get; init; }
        public Location? Location { get; init; }
    }

    readonly struct CasePart
    {
        public INamedTypeSymbol Type { get; init; }
        public string Identifier { get; init; }
        public RefKind Kind { get; init; }
    }
}
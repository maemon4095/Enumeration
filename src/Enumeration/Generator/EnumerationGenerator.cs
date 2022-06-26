using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SourceGeneratorSupplement;
using System.Collections.Immutable;
using System.Reflection;

namespace Enumeration.Generator;

[Generator]
public sealed partial class EnumerationGenerator : IIncrementalGenerator
{
    [TypeSource(typeof(EnumerationAttribute))]
    private static partial string GetAttributeSource();

    static string EnumerationAttributeFullName => $"{nameof(Enumeration)}.{nameof(EnumerationAttribute)}";
    static string ConstructorAttributeFullName => $"{nameof(Enumeration)}.{nameof(ConstructorForAttribute)}";
    static string IndentString => "    ";

    static SymbolDisplayFormat FormatTypeFullPath { get; } = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(this.ProductInitialSource);
        this.RegisterProductSource(context);
    }

    private void ProductInitialSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource(EnumerationAttributeFullName, GetAttributeSource());
    }

    private void RegisterProductSource(IncrementalGeneratorInitializationContext context)
    {
        var enumerationAttributeProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(EnumerationAttributeFullName) ?? throw new NullReferenceException($"{EnumerationAttributeFullName} was not found.");
        });
        var constructorAttributeProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(ConstructorAttributeFullName) ?? throw new NullReferenceException($"{ConstructorAttributeFullName} was not found.");
        });


        var provider = context.SyntaxProvider
            .CreateAttribute(enumerationAttributeProvider)
            .Combine(constructorAttributeProvider)
            .Select((tuple, token) =>
            {
                var ((_, _, symbol, enumerationAttribute, enumerationAttributes), constructorAttribute) = tuple;
                var constructorAttributeData = symbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, constructorAttribute)).ToImmutableArray();
                var constructorAttributes = CreateConstructorResolver(constructorAttributeData);

                return new Bundle
                {
                    Symbol = symbol,
                    EnumerationAttributes = enumerationAttributes,
                    ConstructorResolver = constructorAttributes,
                };
            });

        context.RegisterSourceOutput(provider, this.ProductSource);
    }

    static IImmutableDictionary<INamedTypeSymbol, (IMethodSymbol? ConstructMethod, IMethodSymbol? DeconstructMethod)> CreateConstructorResolver(ImmutableArray<AttributeData> attributes)
    {
#pragma warning disable RS1024
        return attributes
                  .Where(a => a.ConstructorArguments.Length == 2)
                  .Select(a =>
                  {
                      var targetSymbol = a.ConstructorArguments[0].Value as INamedTypeSymbol;
                      var ctorSymbol = a.ConstructorArguments[1].Value as INamedTypeSymbol;

                      if (targetSymbol is null) return default;
                      if (ctorSymbol is null) return default;

                      var (constructMethod, deconstructMethod) = GetConstructorAndDeconstructor(ctorSymbol, targetSymbol);

                      return (TargetSymbol: targetSymbol!, constructMethod, deconstructMethod);
                  })
                  .Where(tuple =>
                  {
                      return tuple != default;
                  })
                  .ToImmutableDictionary(
                  tuple => tuple.TargetSymbol,
                  tuple =>
                  {
                      var (_, constructor, deconstructor) = tuple;
                      return (constructor, deconstructor);
                  },
                  new NamedTypeSymbolEqualityComparer());

#pragma warning restore RS1024

        static bool IsConstructorMethod(IMethodSymbol method, INamedTypeSymbol type)
        {
            if (method.IsGenericMethod) return false;
            if (method.ReturnsByRef || method.ReturnsByRefReadonly) return false;
            if (method.Name != "Construct") return false;
            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, type)) return false;
            if (method.Parameters.Any(p => p.RefKind is not (RefKind.In or RefKind.None))) return false;
            return true;
        }
        static bool IsDeconstructorMethod(IMethodSymbol method, INamedTypeSymbol type)
        {
            if (method.IsGenericMethod) return false;
            if (!method.ReturnsVoid) return false;
            if (method.Name != "Deconstruct") return false;
            if (method.Parameters.Length <= 0) return false;
            if (method.Parameters[0].RefKind is not (RefKind.In or RefKind.None)) return false;
            if (method.Parameters.Skip(1).Any(p => p.RefKind is not RefKind.Out)) return false;
            return true;
        }

        static (IMethodSymbol? Constructor, IMethodSymbol? Deconstructor) GetConstructorAndDeconstructor(INamedTypeSymbol type, INamedTypeSymbol target)
        {
            var members = type.GetMembers().OfType<IMethodSymbol>();
            var constructorCandidates = members.Concat(target.Constructors).Where(m => IsConstructorMethod(m, target));
            var deconstructor = members.FirstOrDefault(m => IsDeconstructorMethod(m, target));
        }
    }


    private void ProductSource(SourceProductionContext context, Bundle bundle)
    {
        var writer = new IndentedWriter(IndentString);
    }

    readonly struct Bundle
    {
        public ISymbol Symbol { get; init; }
        public ImmutableArray<AttributeData> EnumerationAttributes { get; init; }
        public IImmutableDictionary<INamedTypeSymbol, (MethodBase? ConstructMethod, MethodBase? DeconstructMethod)> ConstructorResolver { get; init; }
    }

    class NamedTypeSymbolEqualityComparer : IEqualityComparer<INamedTypeSymbol>
    {
        public bool Equals(INamedTypeSymbol x, INamedTypeSymbol y) => SymbolEqualityComparer.Default.Equals(x, y);
        public int GetHashCode(INamedTypeSymbol obj) => SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}
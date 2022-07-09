using Microsoft.CodeAnalysis;
using SourceGeneratorSupplement;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Enumeration.Generator;

[Generator]
public sealed partial class EnumerationGenerator : IIncrementalGenerator
{
    [TypeSource(typeof(EnumerationAttribute))]
    private static partial string GetAttributeSource();

    static Type EnumerationAttribute => typeof(EnumerationAttribute);
    static Type ConstructorAttribute => typeof(ConstructorAttribute);
    static Type CaseAttribute => typeof(CaseAttribute);
    static string IndentString => "    ";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(this.ProductInitialSource);
        this.RegisterProductSource(context);
    }

    private void ProductInitialSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource(EnumerationAttribute.FullName, GetAttributeSource());
    }

    private void RegisterProductSource(IncrementalGeneratorInitializationContext context)
    {
        var enumerationAttributeProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(EnumerationAttribute.FullName) ?? throw new NullReferenceException($"{EnumerationAttribute.FullName} was not found.");
        });
        var constructorAttributeProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(ConstructorAttribute.FullName) ?? throw new NullReferenceException($"{ConstructorAttribute.FullName} was not found.");
        });
        var caseAttributeProvider = context.CompilationProvider.Select((compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(CaseAttribute.FullName) ?? throw new NullReferenceException($"{CaseAttribute.FullName} was not found.");
        });

        var provider = context.SyntaxProvider
            .CreateAttribute(enumerationAttributeProvider)
            .Combine(constructorAttributeProvider)
            .Combine(caseAttributeProvider)
            .Select((tuple, token) =>
            {
                var (((syntax, model, symbol, _, _), constructorAttribute), caseAttribute) = tuple;
                var constructorAttributeData = symbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, constructorAttribute)).ToImmutableArray();
                var (cases, caseDiagnostic) = CreateCases((symbol as INamedTypeSymbol)!, caseAttribute);
                var constructorResolver = CreateConstructorResolver(constructorAttributeData);

                return new Bundle
                {
                    Symbol = (symbol as INamedTypeSymbol)!,
                    Cases = cases,
                    ConstructorResolver = constructorResolver,
                    Diagnostics = caseDiagnostic is null ? Enumerable.Empty<Diagnostic>() : new[] { caseDiagnostic }
                };
            });

        context.RegisterSourceOutput(provider, this.ProductSource);
    }


    class Bundle
    {
        public INamedTypeSymbol Symbol { get; init; }
        public IEnumerable<Case>? Cases { get; init; }
        public IImmutableDictionary<INamedTypeSymbol, (IMethodSymbol? Ctor, IMethodSymbol? Dtor)>? ConstructorResolver { get; init; }
        public IEnumerable<Diagnostic> Diagnostics { get; init; }
    }
}
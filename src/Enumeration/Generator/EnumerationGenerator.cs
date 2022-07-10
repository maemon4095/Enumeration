using Microsoft.CodeAnalysis;
using SourceGeneratorSupplement;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Enumeration.Generator;

[Generator]
public sealed partial class EnumerationGenerator : IIncrementalGenerator
{
    [TypeSource(typeof(EnumerationAttribute))]
    private static partial string EnumerationAttributeSource();


    [TypeSource(typeof(ConstructorAttribute))]
    private static partial string ConstructorAttributeSource();

    [TypeSource(typeof(CaseAttribute))]
    private static partial string CaseAttributeSource();

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
        context.AddSource(EnumerationAttribute.FullName, EnumerationAttributeSource());
        context.AddSource(ConstructorAttribute.FullName, ConstructorAttributeSource());
        context.AddSource(CaseAttribute.FullName, CaseAttributeSource());
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
                var context = new PreprocessContext(syntax, (symbol as INamedTypeSymbol)!);
                var constructorAttributeData = symbol.GetAttributes().Where(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, constructorAttribute)).ToImmutableArray();
                var cases = CreateCases((symbol as INamedTypeSymbol)!, caseAttribute, ref context);
                var constructorResolver = CreateConstructorResolver(constructorAttributeData, ref context);

                return new Bundle
                {
                    Symbol = context.Symbol,
                    Cases = cases,
                    ConstructorResolver = constructorResolver,
                    Diagnostics = context.ExportDiagnostics()
                };
            });

        context.RegisterSourceOutput(provider, this.ProductSource);
    }



    struct PreprocessContext
    {
        public SyntaxNode Syntax { get; }
        public INamedTypeSymbol Symbol { get; }

        ImmutableArray<Diagnostic>.Builder builder = ImmutableArray.CreateBuilder<Diagnostic>();

        public PreprocessContext(SyntaxNode syntax, INamedTypeSymbol symbol)
        {
            this.Syntax = syntax;
            this.Symbol = symbol;
        }

        public void AddDiagnostic(Diagnostic diagnostic)
        {
            this.builder.Add(diagnostic);
        }

        public ImmutableArray<Diagnostic> ExportDiagnostics() => this.builder.ToImmutable();
    }
    class Bundle
    {
        public INamedTypeSymbol Symbol { get; init; }
        public IEnumerable<Case>? Cases { get; init; }
        public IImmutableDictionary<INamedTypeSymbol, (IMethodSymbol? Ctor, IMethodSymbol? Dtor)>? ConstructorResolver { get; init; }
        public ImmutableArray<Diagnostic> Diagnostics { get; init; }
    }
}
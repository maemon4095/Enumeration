using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Enumeration.Generator;

[Generator]
public sealed class EnumerationGenerator : IIncrementalGenerator
{
    const string AttributeNamespace = nameof(Enumeration);
    const string AttributeName = "EnumerationAttribute";
    const string AttributeFullName = $"{AttributeNamespace}.{AttributeName}";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateInitialCode);

        var attributeSymbolProvider = context.CompilationProvider.Select(static (compilation, token) =>
        {
            token.ThrowIfCancellationRequested();
            return compilation.GetTypeByMetadataName(AttributeFullName) ?? throw new NullReferenceException();
        });

        RegisterCodeGenerator(context, attributeSymbolProvider);
    }

    static void GenerateInitialCode(IncrementalGeneratorPostInitializationContext context)
    {
        var token = context.CancellationToken;
        token.ThrowIfCancellationRequested();
        var template = new EnumerationAttributeTemplate();
        context.AddSource($"{AttributeFullName}.g.cs", template.TransformText());
    }

    static bool SymbolEquals(ISymbol? left, ISymbol? right) => SymbolEqualityComparer.Default.Equals(left, right);

    static void RegisterCodeGenerator(IncrementalGeneratorInitializationContext context, IncrementalValueProvider<INamedTypeSymbol> attributeSymbolProvider)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, token) =>
            {
                token.ThrowIfCancellationRequested();
                return node is TypeDeclarationSyntax and { AttributeLists.Count: > 0 };
            },
            static (context, token) =>
            {
                token.ThrowIfCancellationRequested();
                var syntax = (TypeDeclarationSyntax)context.Node;
                if (context.SemanticModel.GetDeclaredSymbol(syntax, token) is not INamedTypeSymbol symbol) return default;
                var attributes = symbol.GetAttributes();
                return (symbol, attributes);
            })
            .Where(static tuple => tuple != default).Combine(attributeSymbolProvider)
            .Select(static (tuple, token) =>
            {
                token.ThrowIfCancellationRequested();
                var ((symbol, attributes), attributeSymbol) = tuple;
                if (!attributes.Any(attribute => SymbolEquals(attribute.AttributeClass, attributeSymbol))) return default;
                return symbol;
            })
            .Where(static symbol => symbol is not null)!;

        context.RegisterSourceOutput(syntaxProvider, static (context, symbol) =>
        {
            var options = new EnumerationOptions(symbol);
            context.AddSource(
                $"{Helper.EscapedFullNameOf(options.Symbol)}.g.cs",
                symbol.IsValueType
                    ? new EnumerationStructTemplate(options).TransformText()
                    : new EnumerationClassTemplate(options).TransformText()
            );
            context.AddSource(
                $"{Helper.EscapedFullNameOf(options.Symbol)}_Extension.g.cs",
                new EnumerationExtensionTemplate(options).TransformText()                  
            );
        });
    }
}
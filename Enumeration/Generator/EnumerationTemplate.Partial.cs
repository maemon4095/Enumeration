using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Enumeration.Generator;

readonly struct EnumerationOptions
{
    static SymbolDisplayFormat NameOnly { get; } = new SymbolDisplayFormat(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

    public EnumerationOptions(INamedTypeSymbol symbol)
    {
        var namespaceSymbol = symbol.ContainingNamespace;
        this.Namespace = namespaceSymbol.IsGlobalNamespace ? null : namespaceSymbol.ToDisplayString();
        this.Identifier = Helper.FullNameOf(symbol);
        this.Symbol = symbol;
        this.Name = symbol.ToDisplayString(NameOnly);
        var methods = symbol.GetMembers().OfType<IMethodSymbol>()
                            .Where(member => member.DeclaredAccessibility == Accessibility.Public && member.IsStatic && member.IsPartialDefinition && !member.IsGenericMethod)
                            .ToImmutableArray();
        this.Methods = methods;
        var syntax = (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as TypeDeclarationSyntax) ?? throw new NotSupportedException();
        this.Syntax = syntax;
        this.TypeParameterConstraints = syntax.ConstraintClauses;

        var referenceTypeCount = 0;
        var types = new Dictionary<ITypeSymbol, (int Count, int Temp)>(SymbolEqualityComparer.Default);
        foreach (var method in methods)
        {
            var refCount = 0;
            foreach (var param in method.Parameters)
            {
                var type = param.Type;
                if (type.IsReferenceType)
                {
                    refCount++;
                    continue;
                }
                if (type is not ITypeParameterSymbol && type.IsUnmanagedType) continue;
                var contains = types.TryGetValue(type, out var pair);
                pair.Temp++;
                if (contains) types[type] = pair;
                else types.Add(type, pair);
            }
            if (referenceTypeCount < refCount) referenceTypeCount = refCount;


            var keys = types.Keys;
            for (var i = 0; i < keys.Count; ++i)
            {
                var key = keys.ElementAt(i);
                var (count, temp) = types[key];
                types[key] = (Math.Max(count, temp), 0);
            }
        }

        this.SerialTypes = types.Select(pair => (pair.Key, pair.Value.Count)).ToImmutableArray();
        this.ReferenceTypeCount = referenceTypeCount;
    }

    public ImmutableArray<(ITypeSymbol Type, int Count)> SerialTypes { get; }
    public int ReferenceTypeCount { get; }
    public string? Namespace { get; }
    public string Name { get; }
    public string Identifier { get; }
    public INamedTypeSymbol Symbol { get; }
    public ImmutableArray<IMethodSymbol> Methods { get; }
    public string AccessibilityString => SyntaxFacts.GetText(this.Symbol.DeclaredAccessibility);
    public bool IsNamespaceSpecified => !string.IsNullOrEmpty(this.Namespace);
    public TypeDeclarationSyntax Syntax { get; }
    public SyntaxList<TypeParameterConstraintClauseSyntax> TypeParameterConstraints { get; }

    public string DeconstructMethodParamsOf(IMethodSymbol method)
    {
        var builder = new StringBuilder();
        builder.Append(this.Identifier);
        builder.Append(" __self");

        foreach (var parameter in method.Parameters)
        {
            builder.Append(", out ");
            builder.Append(Helper.FullNameOf(parameter.Type));
            builder.Append(' ');
            builder.Append(parameter.Name);
        }

        return builder.ToString();
    }
}

static class Helper
{
    public static bool SymbolEquals(ISymbol? left, ISymbol? right)
    {
        return SymbolEqualityComparer.Default.Equals(left, right);
    }

    public static string ParamsOf(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => $"{FullNameOf(p.Type)} {p.Name}"));
    public static string OutParamsOf(IMethodSymbol method) => string.Join(", ", method.Parameters.Select(p => $"out {FullNameOf(p.Type)} {p.Name}"));

    public static string FullNameOf(ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    public static string EscapedFullNameOf(ISymbol symbol) => FullNameOf(symbol).Replace("global::", "").Replace(".", "_").Replace("<", "_").Replace(",", "_").Replace(" ", "").Replace(">", "");

    public static string IdentifierOf(IMethodSymbol method)
    {
        if (method.ContainingSymbol is not INamedTypeSymbol classSymbol) return method.Name;
        if (classSymbol.TypeParameters.IsEmpty) return method.Name;
        return $"{method.Name}<{string.Join(", ", classSymbol.TypeParameters)}>";
    }

    public static bool IsSerialType(ITypeSymbol type)
    {
        if (type.IsReferenceType) return false;
        if (type is not ITypeParameterSymbol && type.IsUnmanagedType) return false;
        return true;
    }
}




partial class EnumerationStructTemplate
{
    readonly EnumerationOptions Options;

    public EnumerationStructTemplate(EnumerationOptions options)
    {
        this.Options = options;
    }
}


partial class EnumerationClassTemplate
{
    readonly EnumerationOptions Options;

    public EnumerationClassTemplate(EnumerationOptions options)
    {
        this.Options = options;
    }
}

partial class EnumerationExtensionTemplate
{
    readonly EnumerationOptions Options;

    public EnumerationExtensionTemplate(EnumerationOptions options)
    {
        this.Options = options;
    }
}
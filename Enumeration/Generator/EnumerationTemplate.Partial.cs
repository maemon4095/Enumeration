using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Text;

namespace Enumeration.Generator;

readonly struct EnumerationOptions
{
    public EnumerationOptions(INamedTypeSymbol symbol)
    {
        var namespaceSymbol = symbol.ContainingNamespace;
        this.Namespace = namespaceSymbol.IsGlobalNamespace ? null : namespaceSymbol.ToDisplayString();
        this.Name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Replace("global::", "");
        this.Identifier = Helper.FullNameOf(symbol);
        this.Symbol = symbol;
        var methods = symbol.GetMembers().OfType<IMethodSymbol>()
                            .Where(member => member.DeclaredAccessibility == Accessibility.Public && member.IsStatic && member.IsPartialDefinition && !member.IsGenericMethod)
                            .ToImmutableArray();
        this.Methods = methods;

        var referenceTypeCount = 0;
        var types = new List<ITypeSymbol>();
        var buffer = new List<ITypeSymbol?>();
        foreach (var method in methods)
        {
            referenceTypeCount = Math.Max(referenceTypeCount, method.Parameters.Count(p => p.Type.IsReferenceType));
            var parameters = method.Parameters.Select(p => p.Type)
                                              .Where(t => !t.IsReferenceType)
                                              .Where(t => t is ITypeParameterSymbol || !t.IsUnmanagedType);
            buffer.AddRange(parameters);
            foreach (var type in types)
            {
                var idx = buffer.FindIndex(t => t is not null && SymbolEquals(t, type));
                if (idx < 0) continue;
                buffer[idx] = null;
            }
            types.AddRange(buffer.Where(t => t is not null)!);
            buffer.Clear();
        }

        this.OtherTypes = types.GroupBy(t => t, SymbolEqualityComparer.Default).Select(g => (g.Key!, g.Count()))!.ToImmutableArray();
        this.ReferenceTypeCount = referenceTypeCount;
        var syntax = (symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as TypeDeclarationSyntax) ?? throw new NotSupportedException();
        this.Syntax = syntax;
        this.TypeParameterConstraints = syntax.ConstraintClauses;
    }

    public ImmutableArray<(ISymbol Symbol, int Count)> OtherTypes { get; }
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

    static bool SymbolEquals(ISymbol left, ISymbol right)
    {
        return SymbolEqualityComparer.Default.Equals(left, right);
    }

    static bool IsManaged(ITypeSymbol type) => (type is not INamedTypeSymbol named || !named.IsGenericType) && type.IsUnmanagedType;

    public string FieldPathOf(IParameterSymbol parameter)
    {
        var method = (parameter.ContainingSymbol as IMethodSymbol)!;

        if (IsManaged(parameter.Type))
        {
            if (parameter.Type.IsReferenceType)
            {
                var index = method.Parameters.Count(p => p.Ordinal < parameter.Ordinal && p.Type.IsReferenceType);
                return $"this.managed.__reference_{index}";
            }
            else
            {
                var index = method.Parameters.Count(p => p.Ordinal < parameter.Ordinal && SymbolEquals(p.Type, parameter.Type));
                return $"this.managed.{Helper.EscapedFullNameOf(parameter.Type)}_{index}";
            }
        }
        else
        {
            return $"this.unmanaged.{method.Name}.{parameter.Name}";
        }
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
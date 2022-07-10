using Microsoft.CodeAnalysis;
using SourceGeneratorSupplement;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Enumeration.Generator;
partial class EnumerationGenerator
{
    static IImmutableDictionary<INamedTypeSymbol, (IMethodSymbol? ConstructMethod, IMethodSymbol? DeconstructMethod)>? CreateConstructorResolver(ImmutableArray<AttributeData> attributes, ref PreprocessContext context)
    {
        try
        {
#pragma warning disable RS1024
            return attributes.Where(a => a.ConstructorArguments.Length == 2)
                         .Select(a =>
                         {
                             if (a.ConstructorArguments[0].Value is not INamedTypeSymbol targetSymbol) return default;
                             if (a.ConstructorArguments[1].Value is not INamedTypeSymbol ctorSymbol) return default;

                             var (constructMethod, deconstructMethod) = GetCtorAndDtor(ctorSymbol, targetSymbol);

                             return (TargetSymbol: targetSymbol!, constructMethod, deconstructMethod);
                         })
                         .Where(tuple =>
                         {
                             return tuple is (not null, not null, not null);
                         })
                         .ToImmutableDictionary(
                         tuple => tuple.TargetSymbol,
                         tuple =>
                         {
                             var (_, ctor, dtor) = tuple;
                             return (ctor, dtor);
                         },
                         new NamedTypeSymbolEqualityComparer());
#pragma warning restore RS1024
        }
        catch (ArgumentException)
        {
            var diag = Diagnostics.ConstructorDuplicationError(context.Syntax.GetLocation(), context.Symbol);
            context.AddDiagnostic(diag);
            return null;
        }

        static (IMethodSymbol? Ctor, IMethodSymbol? Dtor) GetCtorAndDtor(INamedTypeSymbol ctorType, INamedTypeSymbol target)
        {
            var members = ctorType.GetMembers().OfType<IMethodSymbol>();
            var candidates = members.Product(members).Concat(target.Constructors.Product(target.GetMembers().OfType<IMethodSymbol>()));
            var candidate = candidates.FirstOrDefault((tuple) =>
            {
                var (ctor, dtor) = tuple;
                if (!IsConstructorMethod(ctor, target) || !IsDeconstructorMethod(dtor, target)) return false;
                var ctorParamTypes = ctor.Parameters.Select(p => p.Type);
                var dtorParamTypes = dtor.Parameters.Select(p => p.Type);
                if (SymbolEqualityComparer.Default.Equals(dtor.ContainingType, target))
                {
                    dtorParamTypes = dtorParamTypes.Skip(1);
                }
                return ctorParamTypes.SequenceEqual(dtorParamTypes, SymbolEqualityComparer.Default);
            });

            return candidate;
        }
        static bool IsConstructorMethod(IMethodSymbol method, INamedTypeSymbol type)
        {
            const string MethodName = "Construct";
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, type))
            {
                return method.MethodKind is MethodKind.Constructor;
            }
            if (method.MethodKind is not MethodKind.Ordinary) return false;
            if (method.IsGenericMethod) return false;
            if (method.ReturnsByRef || method.ReturnsByRefReadonly) return false;
            if (method.Name != MethodName) return false;
            if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, type)) return false;
            if (method.Parameters.Any(p => p.RefKind is not (RefKind.In or RefKind.None))) return false;
            return true;
        }
        static bool IsDeconstructorMethod(IMethodSymbol method, INamedTypeSymbol type)
        {
            const string MethodName = "Deconstruct";
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, type))
            {
                if (method.MethodKind is not MethodKind.Ordinary) return false;
                if (method.IsStatic) return false;
                if (!method.ReturnsVoid) return false;
                if (method.Name != MethodName) return false;
                if (method.Parameters.Any(p => p.RefKind is not RefKind.Out)) return false;
                return true;
            }

            if (method.MethodKind is not MethodKind.Ordinary) return false;
            if (method.IsGenericMethod) return false;
            if (!method.ReturnsVoid) return false;
            if (method.Name != MethodName) return false;
            if (method.Parameters.Length <= 0) return false;
            if (method.Parameters[0].RefKind is not (RefKind.In or RefKind.None)) return false;
            if (method.Parameters.Skip(1).Any(p => p.RefKind is not RefKind.Out)) return false;
            return true;
        }
    }
    class NamedTypeSymbolEqualityComparer : IEqualityComparer<INamedTypeSymbol>
    {
        public bool Equals(INamedTypeSymbol x, INamedTypeSymbol y) => SymbolEqualityComparer.Default.Equals(x, y);
        public int GetHashCode(INamedTypeSymbol obj) => SymbolEqualityComparer.Default.GetHashCode(obj);
    }
}
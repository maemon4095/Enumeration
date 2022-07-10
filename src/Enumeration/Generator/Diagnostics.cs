using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Enumeration.Generator;
internal class Diagnostics
{
    static string IdBase { get; } = "EnumerationGenerator";
    static string Title { get; } = "Enumeration generator diagnostic";
    static string Category { get; } = "Enumeration.Usage";
    

    static DiagnosticDescriptor CaseDuplicationErrorDiscriptor { get; } = new DiagnosticDescriptor(
        id: $"{IdBase}_{nameof(CaseDuplicationError)}",
        title: Title,
        messageFormat: "Case identifier must be unique. identifier: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    static DiagnosticDescriptor ConstructorDuplicationErrorDiscriptor { get; } = new DiagnosticDescriptor(
        id: $"{IdBase}_{nameof(ConstructorDuplicationError)}",
        title: Title,
        messageFormat: "Only one constructor can be applied per type. Type: {0}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static Diagnostic CaseDuplicationError(Location? location, string id) => Diagnostic.Create(CaseDuplicationErrorDiscriptor, location, id);

    public static Diagnostic ConstructorDuplicationError(Location? location, INamedTypeSymbol type) => Diagnostic.Create(ConstructorDuplicationErrorDiscriptor, location, type);
}

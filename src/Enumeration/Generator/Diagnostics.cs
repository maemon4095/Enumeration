using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Enumeration.Generator;
internal class Diagnostics
{
    static string IdBase { get; } = "EnumerationGenerator";
    static string Title { get; } = "Enumeration generator diagnostic";
    

    static DiagnosticDescriptor CaseDuplicationErrorDiscriptor { get; } = new DiagnosticDescriptor(
        id: $"{IdBase}_{nameof(CaseDuplicationError)}",
        title: Title,
        messageFormat: "Case identifier must be unique. identifier: {0}",
        category: "Enumeration.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static Diagnostic CaseDuplicationError(Location? location, string id) => Diagnostic.Create(CaseDuplicationErrorDiscriptor, location, id);
}

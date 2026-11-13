using Microsoft.CodeAnalysis;

namespace PureMarkup.Maui.Generators;

internal static class Diagnostics
{
    private const string Category = "PureMarkup";

    public static readonly DiagnosticDescriptor ReadOnly = Create(
        "PM0001", "Read-only bindable property skipped", "'{0}' is read-only; no markup method generated");

    public static readonly DiagnosticDescriptor DelegateType = Create(
        "PM0002", "Delegate-typed property skipped", "'{0}' has a delegate type; no markup method generated");

    public static readonly DiagnosticDescriptor NameCollision = Create(
        "PM0003", "Name collision skipped", "'{0}' collides with '{1}'; no markup method generated");

    public static readonly DiagnosticDescriptor GenericType = Create(
        "PM0004", "Generic type skipped", "'{0}' is generic; no markup methods generated for it");

    public static readonly DiagnosticDescriptor UnsupportedAttachedTarget = Create(
        "PM0005", "Attached property target not supported", "'{0}' attaches to '{1}', which is not a BindableObject class; no markup method generated");

    public static readonly DiagnosticDescriptor MauiNotReferenced = Create(
        "PM0006", "Microsoft.Maui.Controls not referenced", "GenerateMarkup needs a reference to Microsoft.Maui.Controls", DiagnosticSeverity.Warning);

    public static DiagnosticDescriptor? Find(string id) => id switch
    {
        "PM0001" => ReadOnly,
        "PM0002" => DelegateType,
        "PM0003" => NameCollision,
        "PM0004" => GenericType,
        "PM0005" => UnsupportedAttachedTarget,
        "PM0006" => MauiNotReferenced,
        _ => null,
    };

    private static DiagnosticDescriptor Create(string id, string title, string format, DiagnosticSeverity severity = DiagnosticSeverity.Info)
        => new(id, title, format, Category, severity, isEnabledByDefault: true);
}

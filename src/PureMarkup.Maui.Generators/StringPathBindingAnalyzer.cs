using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PureMarkup.Maui.Generators;

/// <summary>
/// PM0007: a <c>Microsoft.Maui.Controls.Binding</c> (the string-path binding) passed to a
/// PureMarkup method. Markup methods take <c>BindingBase</c> and never wrap the binding call, so MAUI's
/// compiled-binding interceptor only helps when the caller uses <c>Binding.Create(lambda)</c>.
/// MAUI marks the string-path constructors <c>[RequiresUnreferencedCode]</c>, but MAUI's build turns the
/// trim analyzer off unless TrimMode=full or PublishAot, so this warning fires on every build.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringPathBindingAnalyzer : DiagnosticAnalyzer
{
    private const string GeneratorName = "PureMarkup.Maui.Generators";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.StringPathBinding);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(start =>
        {
            var binding = start.Compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.Binding");
            if (binding is null) return;
            start.RegisterOperationAction(c => Analyze(c, binding), OperationKind.Argument);
        });
    }

    private static void Analyze(OperationAnalysisContext context, INamedTypeSymbol binding)
    {
        var argument = (IArgumentOperation)context.Operation;
        if (argument.Parameter?.ContainingSymbol is not IMethodSymbol method || !IsMarkupMethod(method)) return;

        var value = argument.Value;
        while (value is IConversionOperation { IsImplicit: true } conversion) value = conversion.Operand;
        if (!DerivesFrom(value.Type, binding)) return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StringPathBinding, value.Syntax.GetLocation(), method.Name));
    }

    // Library methods (generated and sugar) plus methods a consumer generates with [GenerateMarkup].
    private static bool IsMarkupMethod(IMethodSymbol method)
    {
        if (method.ContainingAssembly?.Name == "PureMarkup.Maui") return true;
        foreach (var attribute in method.ContainingType.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute"
                && attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value as string == GeneratorName)
            {
                return true;
            }
        }
        return false;
    }

    private static bool DerivesFrom(ITypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, baseType)) return true;
        }
        return false;
    }
}

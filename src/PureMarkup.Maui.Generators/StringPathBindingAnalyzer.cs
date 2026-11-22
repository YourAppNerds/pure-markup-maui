using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            var multiBinding = start.Compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.MultiBinding");
            start.RegisterOperationAction(c => Analyze(c, new Types(binding, multiBinding)), OperationKind.Argument);
        });
    }

    private static void Analyze(OperationAnalysisContext context, Types types)
    {
        var argument = (IArgumentOperation)context.Operation;
        if (argument.Parameter?.ContainingSymbol is not IMethodSymbol method || !IsMarkupMethod(method)) return;

        var value = argument.Value;
        while (value is IConversionOperation { IsImplicit: true } conversion) value = conversion.Operand;
        if (!new Search(types, Root(argument)).IsStringPath(value)) return;

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.StringPathBinding, value.Syntax.GetLocation(), method.Name));
    }

    private static IOperation Root(IOperation operation)
    {
        while (operation.Parent is not null) operation = operation.Parent;
        return operation;
    }

    private sealed record Types(INamedTypeSymbol Binding, INamedTypeSymbol? MultiBinding);

    /// <summary>
    /// Whether a value can be a string-path Binding: the value itself, a MultiBinding with such a child,
    /// or a local variable that is assigned one anywhere in the same method body (not flow-sensitive).
    /// Fields, properties, parameters and method results are not followed.
    /// </summary>
    private sealed class Search(Types types, IOperation root)
    {
        private readonly HashSet<ILocalSymbol> _visited = new(SymbolEqualityComparer.Default);

        public bool IsStringPath(IOperation? value)
        {
            while (value is IConversionOperation conversion) value = conversion.Operand;
            switch (value)
            {
                case null:
                    return false;
                case IConditionalOperation conditional:
                    return IsStringPath(conditional.WhenTrue) || IsStringPath(conditional.WhenFalse);
                case ICoalesceOperation coalesce:
                    return IsStringPath(coalesce.Value) || IsStringPath(coalesce.WhenNull);
            }

            if (DerivesFrom(value.Type, types.Binding)) return true;
            if (value is IObjectCreationOperation creation && IsMultiBinding(creation.Type))
            {
                return creation.Initializer is not null && HasStringPathChild(creation.Initializer);
            }

            return value is ILocalReferenceOperation local && LocalIsStringPath(local.Local);
        }

        private bool IsMultiBinding(ITypeSymbol? type) => types.MultiBinding is not null && DerivesFrom(type, types.MultiBinding);

        // Bindings = { ... }, Bindings = new List<BindingBase> { ... }, Bindings = [ ... ], new BindingBase[] { ... }.
        private bool HasStringPathChild(IOperation initializer)
        {
            foreach (var operation in initializer.Descendants())
            {
                var found = operation switch
                {
                    IArgumentOperation argument => IsStringPath(argument.Value),
                    ICollectionExpressionOperation collection => collection.Elements.Any(IsStringPath),
                    IArrayInitializerOperation array => array.ElementValues.Any(IsStringPath),
                    _ => false,
                };
                if (found) return true;
            }

            return false;
        }

        private bool LocalIsStringPath(ILocalSymbol local)
        {
            if (!_visited.Add(local)) return false;
            var isMulti = IsMultiBinding(local.Type);
            foreach (var operation in root.Descendants())
            {
                var found = operation switch
                {
                    IVariableDeclaratorOperation declarator when SymbolEqualityComparer.Default.Equals(declarator.Symbol, local)
                        => IsStringPath(declarator.GetVariableInitializer()?.Value),
                    ISimpleAssignmentOperation { Target: ILocalReferenceOperation target } assignment
                        when SymbolEqualityComparer.Default.Equals(target.Local, local)
                        => IsStringPath(assignment.Value),
                    // multi.Bindings.Add(new Binding("..."))
                    IInvocationOperation { TargetMethod.Name: "Add", Instance: IPropertyReferenceOperation { Property.Name: "Bindings", Instance: ILocalReferenceOperation owner } } add
                        when isMulti && SymbolEqualityComparer.Default.Equals(owner.Local, local)
                        => add.Arguments.Any(a => IsStringPath(a.Value)),
                    _ => false,
                };
                if (found) return true;
            }

            return false;
        }
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

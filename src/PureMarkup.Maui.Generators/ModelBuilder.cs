using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PureMarkup.Maui.Generators;

/// <summary>Turns symbols into the equatable <see cref="MarkupModel"/>. All symbol work lives here.</summary>
internal static class ModelBuilder
{
    public const string AttributeName = "PureMarkup.Maui.GenerateMarkupAttribute";
    private const string DefaultNamespace = "PureMarkup.Maui";
    private const string BindingBaseType = "global::Microsoft.Maui.Controls.BindingBase";
    private const string PresetContextName = "PureMarkup.Maui.PresetContext`1";
    private const string TokenName = "PureMarkup.Maui.Token`1";
    private const string GeneratorName = "PureMarkup.Maui.Generators";

    // Whole-assembly mode skips these namespace segments: framework plumbing and the
    // On<Platform>() platform-specifics API, whose attached properties collide by name.
    private static readonly string[] ExcludedNamespaceSegments = ["Internals", "PlatformConfiguration", "Compatibility", "Design"];

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    private static readonly SymbolDisplayFormat ValueFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <summary>
    /// Cheap per-edit step: the whole-assembly requests that target referenced (metadata) assemblies,
    /// reduced to an equatable key. Edits to user source leave the key unchanged, so the expensive
    /// walk in <see cref="BuildWholeAssembly"/> stays cached.
    /// </summary>
    public static WholeAssemblyInput ReadWholeAssemblyInput(Compilation compilation)
    {
        var requests = ReadRequests(compilation).Where(r => IsWholeAssemblyFromMetadata(r, compilation)).ToList();
        if (requests.Count == 0)
        {
            return new WholeAssemblyInput(EquatableArray<WholeAssemblyKey>.Empty, compilation);
        }

        var mauiAssembly = compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.BindableObject")?.ContainingAssembly.Identity.GetDisplayName(fullKey: true) ?? "";
        var presets = HasPresets(compilation);
        var keys = requests
            .Select(r => new WholeAssemblyKey(
                r.Type.ToDisplayString(TypeFormat),
                r.Type.ContainingAssembly.Identity.GetDisplayName(fullKey: true),
                mauiAssembly,
                r.Namespace,
                presets))
            .ToArray();
        return new WholeAssemblyInput(new EquatableArray<WholeAssemblyKey>(keys), compilation);
    }

    /// <summary>The expensive walk over every public type of referenced assemblies (e.g. all of Microsoft.Maui.Controls).</summary>
    public static MarkupModel BuildWholeAssembly(WholeAssemblyInput input, CancellationToken ct)
    {
        if (input.Keys.Length == 0)
        {
            return MarkupModel.Empty;
        }

        var compilation = input.Compilation;
        return Build(compilation, ReadRequests(compilation).Where(r => IsWholeAssemblyFromMetadata(r, compilation)).ToList(), ct);
    }

    /// <summary>
    /// Everything else: single-type requests and whole-assembly requests on the compilation's own
    /// assembly. These depend on user source, so they run per edit; they are small.
    /// Types already covered by a metadata whole-assembly request are skipped.
    /// </summary>
    public static MarkupModel BuildLocal(Compilation compilation, CancellationToken ct)
    {
        var requests = ReadRequests(compilation);
        var covered = new HashSet<IAssemblySymbol>(
            requests.Where(r => IsWholeAssemblyFromMetadata(r, compilation)).Select(r => r.Type.ContainingAssembly),
            SymbolEqualityComparer.Default);
        var local = requests
            .Where(r => !IsWholeAssemblyFromMetadata(r, compilation) && !covered.Contains(r.Type.ContainingAssembly))
            .ToList();
        return Build(compilation, local, ct, covered);
    }

    /// <summary>The runtime preset types ship in PureMarkup.Maui; without them only control overloads are emitted.</summary>
    private static bool HasPresets(Compilation compilation) =>
        compilation.GetTypeByMetadataName(PresetContextName) is not null && compilation.GetTypeByMetadataName(TokenName) is not null;

    private static bool IsWholeAssemblyFromMetadata(Request request, Compilation compilation)
        => request.WholeAssembly && !SymbolEqualityComparer.Default.Equals(request.Type.ContainingAssembly, compilation.Assembly);

    private static List<Request> ReadRequests(Compilation compilation)
    {
        var requests = new List<Request>();
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != AttributeName
                || attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not INamedTypeSymbol type)
            {
                continue;
            }

            var wholeAssembly = false;
            string? ns = null;
            foreach (var named in attribute.NamedArguments)
            {
                if (named.Key == "WholeAssembly" && named.Value.Value is bool b)
                {
                    wholeAssembly = b;
                }
                else if (named.Key == "Namespace" && named.Value.Value is string s && s.Length > 0)
                {
                    ns = s;
                }
            }

            requests.Add(new Request(type, wholeAssembly, ns));
        }

        return requests;
    }

    private static MarkupModel Build(Compilation compilation, List<Request> requests, CancellationToken ct, HashSet<IAssemblySymbol>? coveredAssemblies = null)
    {
        if (requests.Count == 0)
        {
            return MarkupModel.Empty;
        }

        var diagnostics = new List<DiagnosticModel>();
        var maui = MauiSymbols.Create(compilation);
        if (maui is null)
        {
            diagnostics.Add(Diag(Diagnostics.MauiNotReferenced));
            return new MarkupModel(EquatableArray<TypeModel>.Empty, new EquatableArray<DiagnosticModel>(diagnostics.ToArray()));
        }

        // Each type goes to the namespace of the attribute that names it. A type reached by several
        // attributes keeps the first one, in attribute order.
        var namespaces = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
        foreach (var request in requests)
        {
            var ns = request.Namespace ?? DefaultNamespace;
            if (request.WholeAssembly)
            {
                var collected = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                CollectTypes(request.Type.ContainingAssembly.GlobalNamespace, collected, ct);
                foreach (var type in collected.OrderBy(t => t.ToDisplayString(TypeFormat), StringComparer.Ordinal))
                {
                    AddType(namespaces, type, ns);
                }
            }
            else
            {
                AddType(namespaces, request.Type, ns);
            }
        }

        // A single type also gets the members it inherits from the user's own base classes, up to
        // the first base that already has methods: a MAUI type (compiled into PureMarkup.Maui),
        // a type another attribute names, or a type a referenced assembly generated markup for.
        var generatedInReferences = new Dictionary<IAssemblySymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        foreach (var request in requests.Where(r => !r.WholeAssembly))
        {
            for (var b = request.Type.BaseType; b is not null && !HasMarkupElsewhere(b); b = b.BaseType)
            {
                ct.ThrowIfCancellationRequested();
                AddType(namespaces, b.OriginalDefinition, request.Namespace ?? DefaultNamespace);
            }
        }

        bool HasMarkupElsewhere(INamedTypeSymbol type)
        {
            if (!DerivesFrom(type, maui.BindableObject) || IsMauiType(type) || namespaces.ContainsKey(type.OriginalDefinition))
            {
                return true;
            }

            var assembly = type.ContainingAssembly;
            if (SymbolEqualityComparer.Default.Equals(assembly, compilation.Assembly))
            {
                return false;
            }

            if (coveredAssemblies is not null && coveredAssemblies.Contains(assembly))
            {
                return true;
            }

            if (!generatedInReferences.TryGetValue(assembly, out var generated))
            {
                generated = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                CollectGeneratedReceivers(assembly.GlobalNamespace, generated, ct);
                generatedInReferences.Add(assembly, generated);
            }

            return generated.Contains(type.OriginalDefinition);
        }

        var candidates = new List<Candidate>();
        foreach (var type in namespaces.Keys.OrderBy(t => t.ToDisplayString(TypeFormat), StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            CollectCandidates(type, maui, candidates, diagnostics);
        }

        var accepted = ResolveCollisions(candidates, diagnostics);

        var presets = HasPresets(compilation);
        var visualElement = compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.VisualElement");
        var classNames = new HashSet<string>(StringComparer.Ordinal);
        var typeModels = accepted
            .GroupBy(c => c.DeclaringType, SymbolEqualityComparer.Default)
            .Select(g => (Type: (INamedTypeSymbol)g.Key!, Methods: g.ToList()))
            .OrderBy(g => g.Type.ToDisplayString(TypeFormat), StringComparer.Ordinal)
            .Select(g => new TypeModel(
                namespaces[g.Type],
                UniqueClassName(g.Type, classNames),
                g.Type.ToDisplayString(TypeFormat),
                new EquatableArray<MethodModel>(g.Methods
                    .OrderBy(c => c.Method.MethodName, StringComparer.Ordinal)
                    .ThenBy(c => c.Method.Kind)
                    .Select(c => presets && visualElement is not null
                        && (DerivesFrom(c.Receiver, visualElement) || DerivesFrom(visualElement, c.Receiver))
                        ? c.Method with { PresetTarget = true }
                        : c.Method)
                    .ToArray()),
                presets))
            .ToArray();

        return new MarkupModel(new EquatableArray<TypeModel>(typeModels), new EquatableArray<DiagnosticModel>(diagnostics.ToArray()));
    }

    private static void AddType(Dictionary<INamedTypeSymbol, string> namespaces, INamedTypeSymbol type, string ns)
    {
        if (!namespaces.ContainsKey(type))
        {
            namespaces.Add(type, ns);
        }
    }

    /// <summary>Types of Microsoft.Maui.* (already generated into PureMarkup.Maui), by assembly or by namespace.</summary>
    private static bool IsMauiType(INamedTypeSymbol type)
    {
        if (type.ContainingAssembly?.Name.StartsWith("Microsoft.Maui", StringComparison.Ordinal) == true)
        {
            return true;
        }

        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        return ns == "Microsoft.Maui" || ns.StartsWith("Microsoft.Maui.", StringComparison.Ordinal);
    }

    /// <summary>Receivers of markup methods that a referenced assembly's own generator run emitted.</summary>
    private static void CollectGeneratedReceivers(INamespaceSymbol ns, HashSet<INamedTypeSymbol> receivers, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        foreach (var type in ns.GetTypeMembers())
        {
            if (!type.IsStatic || !type.GetAttributes().Any(a =>
                    a.AttributeClass?.ToDisplayString() == "System.CodeDom.Compiler.GeneratedCodeAttribute"
                    && a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value as string == GeneratorName))
            {
                continue;
            }

            foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (!method.IsExtensionMethod || method.Parameters.Length == 0)
                {
                    continue;
                }

                var receiver = method.Parameters[0].Type;
                if (receiver is ITypeParameterSymbol parameter)
                {
                    foreach (var constraint in parameter.ConstraintTypes.OfType<INamedTypeSymbol>())
                    {
                        receivers.Add(constraint.OriginalDefinition);
                    }
                }
                else if (receiver is INamedTypeSymbol named)
                {
                    receivers.Add(named.OriginalDefinition);
                }
            }
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            CollectGeneratedReceivers(child, receivers, ct);
        }
    }

    private static void CollectTypes(INamespaceSymbol ns, HashSet<INamedTypeSymbol> types, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (Array.IndexOf(ExcludedNamespaceSegments, ns.Name) >= 0)
        {
            return;
        }

        foreach (var type in ns.GetTypeMembers())
        {
            AddWithNested(type, types);
        }

        foreach (var child in ns.GetNamespaceMembers())
        {
            CollectTypes(child, types, ct);
        }
    }

    private static void AddWithNested(INamedTypeSymbol type, HashSet<INamedTypeSymbol> types)
    {
        if (type.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        types.Add(type);
        foreach (var nested in type.GetTypeMembers())
        {
            AddWithNested(nested, types);
        }
    }

    private static void CollectCandidates(INamedTypeSymbol type, MauiSymbols maui, List<Candidate> candidates, List<DiagnosticModel> diagnostics)
    {
        if (type.TypeKind != TypeKind.Class || !IsPubliclyVisible(type) || IsHidden(type))
        {
            return;
        }

        var isBindable = !type.IsStatic && DerivesFrom(type, maui.BindableObject);
        var found = new List<Candidate>();
        var foundDiagnostics = new List<DiagnosticModel>();

        if (isBindable)
        {
            foreach (var member in type.GetMembers())
            {
                switch (member)
                {
                    case IPropertySymbol property:
                        AddProperty(type, property, maui, found, foundDiagnostics);
                        break;
                    case IEventSymbol evt:
                        AddEvent(type, evt, found);
                        break;
                }
            }
        }

        AddAttached(type, maui, found, foundDiagnostics);

        if (type.IsGenericType)
        {
            // An extension method cannot infer the declaring type's own type arguments from the
            // receiver constraint, so generic types are skipped.
            if (found.Count > 0)
            {
                diagnostics.Add(Diag(Diagnostics.GenericType, type.ToDisplayString()));
            }

            return;
        }

        candidates.AddRange(found);
        diagnostics.AddRange(foundDiagnostics);
    }

    private static void AddProperty(INamedTypeSymbol type, IPropertySymbol property, MauiSymbols maui, List<Candidate> found, List<DiagnosticModel> diagnostics)
    {
        if (property.DeclaredAccessibility != Accessibility.Public || property.IsStatic || property.IsIndexer
            || property.IsOverride || property.ReturnsByRef || property.ReturnsByRefReadonly || IsHidden(property))
        {
            return;
        }

        var field = FindBindableField(type, property.Name + "Property", maui);
        var setter = property.SetMethod;
        if (setter is null || setter.DeclaredAccessibility != Accessibility.Public || setter.IsInitOnly)
        {
            if (field is not null)
            {
                diagnostics.Add(Diag(Diagnostics.ReadOnly, Display(type, property.Name)));
            }

            return;
        }

        if (IsHidden(setter))
        {
            return;
        }

        if (property.Type.TypeKind == TypeKind.Delegate)
        {
            diagnostics.Add(Diag(Diagnostics.DelegateType, Display(type, property.Name)));
            return;
        }

        if (property.Type.IsRefLikeType || property.Type.TypeKind == TypeKind.Pointer)
        {
            return;
        }

        var receiver = type.ToDisplayString(TypeFormat);
        var model = field is not null
            ? new MethodModel(MemberKind.Bindable, Escape(property.Name), receiver, type.IsSealed,
                ValueDisplay(property.Type), field.ContainingType.ToDisplayString(TypeFormat) + "." + field.Name, false)
            : new MethodModel(MemberKind.Plain, Escape(property.Name), receiver, type.IsSealed,
                ValueDisplay(property.Type), Escape(property.Name), false);
        found.Add(new Candidate(model, type, type, (ISymbol?)field ?? property, Display(type, property.Name)));
    }

    private static void AddEvent(INamedTypeSymbol type, IEventSymbol evt, List<Candidate> found)
    {
        if (evt.DeclaredAccessibility != Accessibility.Public || evt.IsStatic || evt.IsOverride || IsHidden(evt)
            || evt.Type is not INamedTypeSymbol delegateType)
        {
            return;
        }

        var invoke = delegateType.DelegateInvokeMethod;
        var actionOverload = invoke is { ReturnsVoid: true, Parameters.Length: 2 };
        var model = new MethodModel(MemberKind.Event, "On" + evt.Name, type.ToDisplayString(TypeFormat), type.IsSealed,
            ValueDisplay(evt.Type), Escape(evt.Name), actionOverload);
        found.Add(new Candidate(model, type, type, evt, Display(type, evt.Name)));
    }

    private static void AddAttached(INamedTypeSymbol type, MauiSymbols maui, List<Candidate> found, List<DiagnosticModel> diagnostics)
    {
        foreach (var member in type.GetMembers())
        {
            if (member is not IMethodSymbol { IsStatic: true, ReturnsVoid: true, IsGenericMethod: false, Parameters.Length: 2 } method
                || method.DeclaredAccessibility != Accessibility.Public
                || method.Name.Length <= 3
                || !method.Name.StartsWith("Set", StringComparison.Ordinal)
                || IsHidden(method))
            {
                continue;
            }

            var name = method.Name.Substring(3);
            var field = type.GetMembers(name + "Property").OfType<IFieldSymbol>()
                .FirstOrDefault(f => f.IsStatic && f.DeclaredAccessibility == Accessibility.Public
                    && SymbolEqualityComparer.Default.Equals(f.Type, maui.BindableProperty));
            if (field is null || IsHidden(field))
            {
                continue;
            }

            // A same-named instance property means XProperty is a normal bindable property.
            if (type.GetMembers(name).OfType<IPropertySymbol>().Any(p => !p.IsStatic))
            {
                continue;
            }

            var target = method.Parameters[0].Type;
            if (target is not INamedTypeSymbol targetType || targetType.TypeKind != TypeKind.Class || !DerivesFrom(targetType, maui.BindableObject))
            {
                // The same property often has a second SetX overload that does take a BindableObject.
                var hasBindableOverload = type.GetMembers(method.Name).OfType<IMethodSymbol>().Any(m =>
                    !SymbolEqualityComparer.Default.Equals(m, method) && m.IsStatic && m.Parameters.Length == 2
                    && m.Parameters[0].Type is INamedTypeSymbol t && DerivesFrom(t, maui.BindableObject));
                if (!hasBindableOverload)
                {
                    diagnostics.Add(Diag(Diagnostics.UnsupportedAttachedTarget, Display(type, name), target.ToDisplayString()));
                }

                continue;
            }

            var valueType = method.Parameters[1].Type;
            if (valueType.TypeKind == TypeKind.Delegate)
            {
                diagnostics.Add(Diag(Diagnostics.DelegateType, Display(type, name)));
                continue;
            }

            var model = new MethodModel(MemberKind.Attached, Escape(name), targetType.ToDisplayString(TypeFormat), targetType.IsSealed,
                ValueDisplay(valueType), type.ToDisplayString(TypeFormat) + "." + name, false);
            found.Add(new Candidate(model, type, targetType, field, Display(type, name)));
        }
    }

    /// <summary>
    /// Removes duplicates and resolves overload ambiguities between methods on related receivers:
    /// colliding attached properties get their declaring type as a prefix (Shell.BackgroundColor
    /// becomes ShellBackgroundColor); anything still colliding is skipped with PM0003, keeping the
    /// method on the most basic receiver.
    /// </summary>
    private static List<Candidate> ResolveCollisions(List<Candidate> candidates, List<DiagnosticModel> diagnostics)
    {
        var ordered = candidates
            .OrderBy(c => Depth(c.Receiver))
            .ThenBy(c => c.Method.Kind == MemberKind.Attached ? 1 : 0)
            .ThenBy(c => c.Label, StringComparer.Ordinal)
            .ToList();

        // Same backing member reached twice (e.g. a 'new' redeclaration sharing the field): keep the first.
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        ordered = ordered.Where(c => seen.Add(c.Backing)).ToList();

        // Rename attached properties that collide with anything.
        var byName = ordered.ToLookup(c => c.Method.MethodName, StringComparer.Ordinal);
        for (var i = 0; i < ordered.Count; i++)
        {
            var c = ordered[i];
            if (c.Method.Kind == MemberKind.Attached && byName[c.Method.MethodName].Any(o => !ReferenceEquals(o, c) && Conflicts(o, c)))
            {
                ordered[i] = c with { Method = c.Method with { MethodName = Prefix(c.DeclaringType) + c.Method.MethodName } };
            }
        }

        var accepted = new List<Candidate>();
        var acceptedByName = new Dictionary<string, List<Candidate>>(StringComparer.Ordinal);
        foreach (var c in ordered)
        {
            if (!acceptedByName.TryGetValue(c.Method.MethodName, out var sameName))
            {
                sameName = [];
                acceptedByName[c.Method.MethodName] = sameName;
            }

            var clash = sameName.FirstOrDefault(a => Conflicts(a, c));
            if (clash is not null)
            {
                diagnostics.Add(Diag(Diagnostics.NameCollision, c.Label, clash.Label));
                continue;
            }

            var shadow = FindPublicMethod(c.Receiver, c.Method.MethodName);
            if (shadow is not null)
            {
                diagnostics.Add(Diag(Diagnostics.NameCollision, c.Label, shadow.ToDisplayString()));
                continue;
            }

            sameName.Add(c);
            accepted.Add(c);
        }

        return accepted;
    }

    private static bool Conflicts(Candidate a, Candidate b)
    {
        if (a.Method.MethodName != b.Method.MethodName)
        {
            return false;
        }

        if (!DerivesFrom(a.Receiver, b.Receiver) && !DerivesFrom(b.Receiver, a.Receiver))
        {
            return false;
        }

        return Signatures(a).Intersect(Signatures(b), StringComparer.Ordinal).Any();
    }

    private static IEnumerable<string> Signatures(Candidate c)
    {
        yield return c.Method.ValueType.TrimEnd('?');
        switch (c.Method.Kind)
        {
            case MemberKind.Bindable or MemberKind.Attached:
                yield return BindingBaseType;
                break;
            case MemberKind.Event when c.Method.ActionOverload:
                yield return "global::System.Action";
                break;
        }
    }

    // A public instance method that can take one argument hides the extension method.
    // Parameterless ones (SearchBar.OnSearchButtonPressed()) do not, so they are ignored.
    private static IMethodSymbol? FindPublicMethod(INamedTypeSymbol type, string name)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var method = t.GetMembers(name).OfType<IMethodSymbol>()
                .FirstOrDefault(m => !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary
                    && m.Parameters.Length > 0);
            if (method is not null)
            {
                return method;
            }
        }

        return null;
    }

    private static IFieldSymbol? FindBindableField(INamedTypeSymbol type, string name, MauiSymbols maui)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            var field = t.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault(f => f.IsStatic
                && f.DeclaredAccessibility == Accessibility.Public
                && SymbolEqualityComparer.Default.Equals(f.Type, maui.BindableProperty));
            if (field is not null)
            {
                return field;
            }
        }

        return null;
    }

    private static string Prefix(INamedTypeSymbol type)
    {
        const string suffix = "Properties";
        var name = type.Name;
        return name.EndsWith(suffix, StringComparison.Ordinal) && name.Length > suffix.Length
            ? name.Substring(0, name.Length - suffix.Length)
            : name;
    }

    private static string UniqueClassName(INamedTypeSymbol type, HashSet<string> used)
    {
        var baseName = type.Name + "Markup";
        for (var c = type.ContainingType; c is not null; c = c.ContainingType)
        {
            baseName = c.Name + "_" + baseName;
        }

        var name = baseName;
        for (var i = 2; !used.Add(name); i++)
        {
            name = baseName + i;
        }

        return name;
    }

    private static string ValueDisplay(ITypeSymbol type)
    {
        // Oblivious reference types (MAUI is only partly annotated) are emitted as nullable,
        // so passing null does not warn in callers that enable nullable.
        if (type.IsReferenceType && type.NullableAnnotation == NullableAnnotation.None)
        {
            type = type.WithNullableAnnotation(NullableAnnotation.Annotated);
        }

        return type.ToDisplayString(ValueFormat);
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var t = type; t is not null; t = t.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(t, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static int Depth(INamedTypeSymbol type)
    {
        var depth = 0;
        for (var t = type.BaseType; t is not null; t = t.BaseType)
        {
            depth++;
        }

        return depth;
    }

    private static bool IsPubliclyVisible(INamedTypeSymbol type)
    {
        for (var t = type; t is not null; t = t.ContainingType)
        {
            if (t.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsHidden(ISymbol symbol)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            switch (attribute.AttributeClass?.ToDisplayString())
            {
                case "System.ObsoleteAttribute":
                    return true;
                case "System.ComponentModel.EditorBrowsableAttribute"
                    when attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is int state && state == 1:
                    return true;
            }
        }

        return false;
    }

    private static string Escape(string name)
        => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    private static string Display(INamedTypeSymbol type, string member) => type.ToDisplayString() + "." + member;

    private static DiagnosticModel Diag(DiagnosticDescriptor descriptor, params string[] args)
        => new(descriptor.Id, new EquatableArray<string>(args));

    private sealed record Request(INamedTypeSymbol Type, bool WholeAssembly, string? Namespace);

    private sealed record Candidate(MethodModel Method, INamedTypeSymbol DeclaringType, INamedTypeSymbol Receiver, ISymbol Backing, string Label);

    private sealed class MauiSymbols
    {
        private MauiSymbols(INamedTypeSymbol bindableObject, INamedTypeSymbol bindableProperty)
        {
            BindableObject = bindableObject;
            BindableProperty = bindableProperty;
        }

        public INamedTypeSymbol BindableObject { get; }

        public INamedTypeSymbol BindableProperty { get; }

        public static MauiSymbols? Create(Compilation compilation)
        {
            var bindableObject = compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.BindableObject");
            var bindableProperty = compilation.GetTypeByMetadataName("Microsoft.Maui.Controls.BindableProperty");
            return bindableObject is null || bindableProperty is null ? null : new MauiSymbols(bindableObject, bindableProperty);
        }
    }
}

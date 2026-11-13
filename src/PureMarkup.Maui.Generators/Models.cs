namespace PureMarkup.Maui.Generators;

internal enum MemberKind
{
    /// <summary>Instance property backed by a BindableProperty: value + binding overloads.</summary>
    Bindable,

    /// <summary>Attached property (static SetX + XProperty): value + binding overloads.</summary>
    Attached,

    /// <summary>Settable property without a BindableProperty: value overload only.</summary>
    Plain,

    /// <summary>Event: handler overload, plus an Action overload for EventHandler shapes.</summary>
    Event,
}

/// <summary>One generated method family. Strings only: no symbols, so the model is cacheable.</summary>
/// <param name="Kind">What kind of member is wrapped.</param>
/// <param name="MethodName">Extension method name.</param>
/// <param name="ReceiverType">Fully qualified receiver type (generic constraint, or the parameter type if sealed).</param>
/// <param name="ReceiverIsSealed">Sealed receivers get a non-generic method (sealed types are not valid constraints).</param>
/// <param name="ValueType">Fully qualified parameter type (property type or event delegate type).</param>
/// <param name="Target">Member access: BindableProperty field, Set method, property or event name.</param>
/// <param name="ActionOverload">Event only: also emit a parameterless Action overload.</param>
internal sealed record MethodModel(
    MemberKind Kind,
    string MethodName,
    string ReceiverType,
    bool ReceiverIsSealed,
    string ValueType,
    string Target,
    bool ActionOverload);

/// <summary>One generated static class: the methods for one declaring type.</summary>
internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    string DeclaringType,
    EquatableArray<MethodModel> Methods);

internal sealed record DiagnosticModel(string Id, EquatableArray<string> Args);

internal sealed record MarkupModel(
    EquatableArray<TypeModel> Types,
    EquatableArray<DiagnosticModel> Diagnostics)
{
    public static MarkupModel Empty { get; } = new(EquatableArray<TypeModel>.Empty, EquatableArray<DiagnosticModel>.Empty);
}

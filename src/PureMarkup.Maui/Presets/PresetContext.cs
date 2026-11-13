using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// Records what a <see cref="Preset{T}"/> sets. The generated methods have an overload on this type
/// with the same names as on controls (<c>p.FontSize(15).TextColor(Tokens.Text)</c>), so presets
/// read like normal markup; each call becomes a <see cref="Setter"/> of a MAUI <see cref="Style"/>.
/// </summary>
/// <typeparam name="T">The element type the preset targets.</typeparam>
public sealed class PresetContext<T>
{
    private readonly bool _nested;

    internal PresetContext(bool nested = false) => _nested = nested;

    internal List<Setter> Setters { get; } = [];

    internal List<StateSetters> States { get; } = [];

    internal List<TriggerBase> Triggers { get; } = [];

    internal List<Action<T>> Actions { get; } = [];

    /// <summary>
    /// Adds a setter. A <see cref="BindingBase"/> value is bound per element; a <see cref="Token"/>
    /// becomes a dynamic resource, so it follows theme changes.
    /// </summary>
    public PresetContext<T> Set(BindableProperty property, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        Setters.Add(new Setter
        {
            Property = property,
            Value = value is Token token ? new Microsoft.Maui.Controls.Internals.DynamicResource(token.Key) : value,
        });
        return this;
    }

    /// <summary>Adds a setter that reads <paramref name="key"/> from the element's resources, live.</summary>
    public PresetContext<T> DynamicResource(BindableProperty property, string key)
    {
        ArgumentNullException.ThrowIfNull(property);
        Setters.Add(new Setter { Property = property, Value = new Microsoft.Maui.Controls.Internals.DynamicResource(key) });
        return this;
    }

    /// <summary>
    /// Setters for a visual state in the <c>CommonStates</c> group (Normal, Disabled, Focused,
    /// PointerOver). State setters beat local values while the state is active.
    /// </summary>
    public PresetContext<T> State(string name, Func<PresetContext<T>, PresetContext<T>> setters) =>
        State(StateSetters.CommonStatesGroup, name, setters);

    /// <summary>Setters for visual state <paramref name="name"/> in <paramref name="group"/>.</summary>
    public PresetContext<T> State(string group, string name, Func<PresetContext<T>, PresetContext<T>> setters)
    {
        States.Add(new StateSetters(group, name, Nested(setters, nameof(State))));
        return this;
    }

    /// <summary>Setters that apply while <paramref name="property"/> equals <paramref name="value"/>.</summary>
    public PresetContext<T> Trigger(BindableProperty property, object? value, Func<PresetContext<T>, PresetContext<T>> setters)
    {
        var trigger = new Trigger(typeof(T)) { Property = property, Value = value };
        foreach (var setter in Nested(setters, nameof(Trigger)))
        {
            trigger.Setters.Add(setter);
        }

        Triggers.Add(trigger);
        return this;
    }

    /// <summary>Setters that apply while <paramref name="binding"/> yields <paramref name="value"/>.</summary>
    public PresetContext<T> DataTrigger(BindingBase binding, object? value, Func<PresetContext<T>, PresetContext<T>> setters)
    {
        var trigger = new DataTrigger(typeof(T)) { Binding = binding, Value = value };
        foreach (var setter in Nested(setters, nameof(DataTrigger)))
        {
            trigger.Setters.Add(setter);
        }

        Triggers.Add(trigger);
        return this;
    }

    /// <summary>
    /// Work a Style cannot hold (events, children, plain properties). Runs on the element at
    /// <c>.Use</c> / <c>.New</c>, in order; it does not run for an implicit default preset.
    /// </summary>
    public PresetContext<T> Also(Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (_nested)
        {
            throw new InvalidOperationException("Also is not allowed inside State or Trigger.");
        }

        Actions.Add(action);
        return this;
    }

    private List<Setter> Nested(Func<PresetContext<T>, PresetContext<T>> setters, string what)
    {
        ArgumentNullException.ThrowIfNull(setters);
        if (_nested)
        {
            throw new InvalidOperationException(what + " is not allowed inside State or Trigger.");
        }

        var inner = new PresetContext<T>(nested: true);
        setters(inner);
        return inner.Setters;
    }
}

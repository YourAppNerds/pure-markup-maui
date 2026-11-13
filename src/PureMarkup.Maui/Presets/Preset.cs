using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(PureMarkup.Maui.PresetHotReload))]

namespace PureMarkup.Maui;

/// <summary>Creates presets.</summary>
public static class Preset
{
    /// <summary>
    /// A preset built from <paramref name="build"/>, written with the normal markup methods. The
    /// compiled <see cref="Style"/> is cached per (base, delegate), so a property getter such as
    /// <c>public static Preset&lt;Entry&gt; Field => Preset.Once&lt;Entry&gt;(p => p.FontSize(15));</c>
    /// returns the same Style every time. Use non-capturing lambdas.
    /// </summary>
    public static Preset<T> Once<T>(Func<PresetContext<T>, PresetContext<T>> build) where T : VisualElement =>
        new(null, build);
}

/// <summary>
/// A reusable look for <typeparamref name="T"/>, compiled to one shared MAUI <see cref="Style"/>.
/// Values set on an element win over the preset in any call order; visual-state and trigger
/// setters win over local values while active (MAUI's setter specificity).
/// </summary>
public sealed class Preset<T> : IEquatable<Preset<T>> where T : VisualElement
{
    private readonly Preset<T>? _base;
    private readonly Func<PresetContext<T>, PresetContext<T>> _build;

    internal Preset(Preset<T>? @base, Func<PresetContext<T>, PresetContext<T>> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        _base = @base;
        _build = build;
    }

    /// <summary>The compiled Style (cached; rebuilt after a Hot Reload update).</summary>
    public Style Style => Compiled.Style;

    internal Compiled<T> Compiled => PresetCache.Get(this, static key => ((Preset<T>)key).Compile());

    /// <summary>A derived preset (<see cref="Style.BasedOn"/>): its setters beat this preset's.</summary>
    public Preset<T> With(Func<PresetContext<T>, PresetContext<T>> build) => new(this, build);

    /// <inheritdoc/>
    public bool Equals(Preset<T>? other) =>
        other is not null && Equals(_base, other._base) && _build.Equals(other._build);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Preset<T>);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_base, _build);

    private Compiled<T> Compile()
    {
        var parent = _base?.Compiled;
        var context = new PresetContext<T>();
        _build(context);

        var style = new Style(typeof(T)) { ApplyToDerivedTypes = true, BasedOn = parent?.Style };
        foreach (var setter in context.Setters)
        {
            style.Setters.Add(setter);
        }

        // Visual states live in one property, so a derived preset merges the base's states with
        // its own instead of replacing the whole list.
        var states = new List<StateSetters>(parent?.States ?? []);
        foreach (var state in context.States)
        {
            var index = states.FindIndex(s => s.Group == state.Group && s.Name == state.Name);
            if (index < 0)
            {
                states.Add(state);
            }
            else
            {
                states[index] = state with { Setters = [.. states[index].Setters, .. state.Setters] };
            }
        }

        if (context.States.Count > 0)
        {
            style.Setters.Add(new Setter { Property = VisualStateManager.VisualStateGroupsProperty, Value = Groups(states) });
        }

        foreach (var trigger in context.Triggers)
        {
            style.Triggers.Add(trigger);
        }

        var actions = new List<Action<T>>(parent?.Actions ?? []);
        actions.AddRange(context.Actions);
        return new Compiled<T>(style, states, actions);
    }

    private static readonly string[] CommonStates = ["Normal", "Disabled", "Focused", "PointerOver"];

    private static VisualStateGroupList Groups(List<StateSetters> states)
    {
        var list = new VisualStateGroupList();
        foreach (var item in states)
        {
            var group = FindExisting(list, item.Group);
            if (group is null)
            {
                group = new VisualStateGroup { Name = item.Group };
                list.Add(group);
            }

            var state = new VisualState { Name = item.Name };
            foreach (var setter in item.Setters)
            {
                state.Setters.Add(setter);
            }

            group.States.Add(state);
        }

        // Without a state for every common state, leaving Disabled (say) finds no target state and
        // keeps the Disabled setters applied. Empty states make every transition clean.
        if (FindExisting(list, StateSetters.CommonStatesGroup) is { } common)
        {
            foreach (var name in CommonStates)
            {
                if (!System.Linq.Enumerable.Any(common.States, s => s.Name == name))
                {
                    common.States.Add(new VisualState { Name = name });
                }
            }
        }

        return list;
    }

    private static VisualStateGroup? FindExisting(VisualStateGroupList list, string name)
    {
        foreach (var group in list)
        {
            if (group.Name == name)
            {
                return group;
            }
        }

        return null;
    }
}

internal sealed record StateSetters(string Group, string Name, List<Setter> Setters)
{
    public const string CommonStatesGroup = "CommonStates";
}

internal sealed record Compiled<T>(Style Style, List<StateSetters> States, List<Action<T>> Actions);

/// <summary>Compiled presets, keyed by preset equality. Cleared on Hot Reload.</summary>
internal static class PresetCache
{
    private static readonly Dictionary<object, object> Cache = [];

    public static int Count
    {
        get
        {
            lock (Cache)
            {
                return Cache.Count;
            }
        }
    }

    public static TValue Get<TValue>(object key, Func<object, TValue> create) where TValue : class
    {
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var value))
            {
                return (TValue)value;
            }
        }

        // Built outside the lock: compiling a derived preset compiles its base first.
        var created = create(key);
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var value))
            {
                return (TValue)value;
            }

            Cache[key] = created;
            return created;
        }
    }

    public static void Clear()
    {
        lock (Cache)
        {
            Cache.Clear();
        }
    }
}

/// <summary>
/// Hot Reload: an edited preset lambda keeps its delegate identity, so the cache would keep the old
/// Style. Clearing it makes elements created after the edit pick up the new setters (existing
/// elements keep their Style; MAUI setters do not update live).
/// </summary>
internal static class PresetHotReload
{
    public static void ClearCache(Type[]? updatedTypes) => PresetCache.Clear();
}

/// <summary>Applying presets to elements and registering app-wide defaults.</summary>
public static class PresetExtensions
{
    /// <summary>
    /// Sets the element's Style to <paramref name="preset"/> (replacing any earlier <c>.Use</c>) and
    /// runs its <c>.Also</c> actions. Local values win regardless of call order.
    /// </summary>
    public static TElement Use<TElement, TPreset>(this TElement self, Preset<TPreset> preset)
        where TElement : TPreset
        where TPreset : VisualElement
    {
        ArgumentNullException.ThrowIfNull(preset);
        var compiled = preset.Compiled;
        self.Style = compiled.Style;
        foreach (var action in compiled.Actions)
        {
            action(self);
        }

        return self;
    }

    /// <summary>Shorthand for <c>new T().Use(preset)</c>.</summary>
    public static T New<T>(this Preset<T> preset) where T : VisualElement, new() => new T().Use(preset);

    /// <summary>
    /// Registers <paramref name="preset"/> as the implicit style for <typeparamref name="T"/> (and
    /// derived types) in <paramref name="resources"/>: a plain <c>new T()</c> under these resources
    /// gets the look. An explicit <c>.Use</c> layers on top and wins where both set a property.
    /// </summary>
    public static ResourceDictionary Default<T>(this ResourceDictionary resources, Preset<T> preset) where T : VisualElement
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(preset);
        resources[typeof(T).FullName!] = preset.Style;
        return resources;
    }

    /// <summary>
    /// App setup in one call: writes theme tokens into <c>app.Resources</c>, keeps them in step with
    /// <see cref="Application.RequestedThemeChanged"/>, then runs <paramref name="defaults"/>
    /// (for example <c>r => r.Default(AppPresets.Entry)</c>).
    /// </summary>
    public static TApp UsePresets<TApp>(this TApp app, Action<ResourceDictionary>? defaults = null) where TApp : Application
    {
        ArgumentNullException.ThrowIfNull(app);
        Theme.Attach(app);
        defaults?.Invoke(app.Resources);
        return app;
    }
}

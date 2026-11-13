using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// A theme token: a value with a light and a dark variant. Pass it to any generated method
/// (<c>.TextColor(Tokens.Text)</c>) on an element or in a preset; it becomes a dynamic resource,
/// and <see cref="Theme"/> rewrites the resource when the app theme changes.
/// </summary>
public abstract class Token
{
    private static readonly List<Token> All = [];
    private static int _next;

    private protected Token(string? name)
    {
        Key = "PureMarkup.Token." + (name ?? "") + "#" + Interlocked.Increment(ref _next);
        lock (All)
        {
            All.Add(this);
        }
    }

    /// <summary>The resource key the token's current value is stored under.</summary>
    public string Key { get; }

    /// <summary>The value for <paramref name="theme"/> (Unspecified means Light).</summary>
    public abstract object? ValueFor(AppTheme theme);

    /// <summary>Used by the generated token overloads: makes <paramref name="property"/> follow the token.</summary>
    public static void Apply(BindableObject target, BindableProperty property, Token token)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(token);
        if (target is Element element)
        {
            element.SetDynamicResource(property, token.Key);
        }
        else
        {
            target.SetValue(property, token.ValueFor(AppTheme.Light));
        }
    }

    internal static Token[] Snapshot()
    {
        lock (All)
        {
            return [.. All];
        }
    }
}

/// <summary>A typed theme token.</summary>
/// <typeparam name="TValue">The property type it feeds.</typeparam>
public sealed class Token<TValue> : Token
{
    /// <summary>A token with separate light and dark values.</summary>
    public Token(TValue light, TValue dark, string? name = null)
        : base(name)
    {
        Light = light;
        Dark = dark;
        Theme.Register(this);
    }

    /// <summary>A token with one value for both themes (spacing, font sizes).</summary>
    public Token(TValue value, string? name = null)
        : this(value, value, name)
    {
    }

    /// <summary>Value in the light theme.</summary>
    public TValue Light { get; }

    /// <summary>Value in the dark theme.</summary>
    public TValue Dark { get; }

    /// <inheritdoc/>
    public override object? ValueFor(AppTheme theme) => theme == AppTheme.Dark ? Dark : Light;
}

/// <summary>
/// Live theme switching for tokens. <c>AppThemeBinding</c> is internal in MAUI on net10, so tokens
/// are dynamic resources instead: <see cref="Apply"/> writes every token's value for a theme into a
/// <see cref="ResourceDictionary"/>, and MAUI pushes the change to every element using it.
/// </summary>
public static class Theme
{
    private static readonly List<(WeakReference<ResourceDictionary> Resources, AppTheme Theme)> Targets = [];

    /// <summary>
    /// Writes all tokens' values for <paramref name="theme"/> into <paramref name="resources"/>.
    /// Tokens created later are written there too.
    /// </summary>
    public static void Apply(ResourceDictionary resources, AppTheme theme)
    {
        ArgumentNullException.ThrowIfNull(resources);
        lock (Targets)
        {
            Targets.RemoveAll(t => !t.Resources.TryGetTarget(out var r) || ReferenceEquals(r, resources));
            Targets.Add((new WeakReference<ResourceDictionary>(resources), theme));
        }

        foreach (var token in Token.Snapshot())
        {
            resources[token.Key] = token.ValueFor(theme);
        }
    }

    /// <summary>
    /// Applies the app's current theme to <c>app.Resources</c> and follows
    /// <see cref="Application.RequestedThemeChanged"/> (OS theme and <see cref="Application.UserAppTheme"/>).
    /// </summary>
    public static void Attach(Application app)
    {
        ArgumentNullException.ThrowIfNull(app);
        Apply(app.Resources, app.RequestedTheme);
        app.RequestedThemeChanged += (_, e) => Apply(app.Resources, e.RequestedTheme);
    }

    internal static void Register(Token token)
    {
        List<(ResourceDictionary Resources, AppTheme Theme)> live = [];
        lock (Targets)
        {
            foreach (var (weak, theme) in Targets)
            {
                if (weak.TryGetTarget(out var resources))
                {
                    live.Add((resources, theme));
                }
            }
        }

        foreach (var (resources, theme) in live)
        {
            resources[token.Key] = token.ValueFor(theme);
        }
    }
}

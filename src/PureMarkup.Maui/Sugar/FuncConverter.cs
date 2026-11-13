// Shape adapted from CommunityToolkit.Maui.Markup (FuncConverter), MIT;
// see THIRD-PARTY-NOTICES.md.
using System.Globalization;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// An <see cref="IValueConverter"/> from delegates, for
/// <c>Binding.Create(getter, converter: new FuncConverter&lt;int, string&gt;(i =&gt; $"{i}"))</c>.
/// Pass <paramref name="convertBack"/> for two-way bindings; without it ConvertBack throws.
/// A null value converts as <c>default(TSource)</c>; a value of another type throws.
/// </summary>
public sealed class FuncConverter<TSource, TTarget>(Func<TSource, TTarget> convert, Func<TTarget, TSource>? convertBack = null)
    : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        convert(Cast<TSource>(value));

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        convertBack is null
            ? throw new NotSupportedException($"{nameof(FuncConverter<TSource, TTarget>)} was created without convertBack.")
            : convertBack(Cast<TTarget>(value));

    private static T Cast<T>(object? value) => value switch
    {
        T t => t,
        null => default!,
        _ => throw new ArgumentException($"Expected {typeof(T).Name}, got {value.GetType().Name}.", nameof(value)),
    };
}

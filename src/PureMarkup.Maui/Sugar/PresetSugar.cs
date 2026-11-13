using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// The <see cref="LayoutSugar"/> shorthands on <see cref="PresetContext{T}"/>, so presets use the same
/// words as element code. Padding shorthands are per type on elements; in a preset write
/// <c>.Padding(new Thickness(h, v))</c>.
/// </summary>
public static class PresetSugar
{
    /// <summary>Sets <see cref="VisualElement.HeightRequest"/>.</summary>
    public static PresetContext<T> Height<T>(this PresetContext<T> self, double value) where T : VisualElement =>
        self.Set(VisualElement.HeightRequestProperty, value);

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/>.</summary>
    public static PresetContext<T> Width<T>(this PresetContext<T> self, double value) where T : VisualElement =>
        self.Set(VisualElement.WidthRequestProperty, value);

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/>.</summary>
    public static PresetContext<T> Size<T>(this PresetContext<T> self, double width, double height) where T : VisualElement =>
        self.Width(width).Height(height);

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/> to one value.</summary>
    public static PresetContext<T> Size<T>(this PresetContext<T> self, double size) where T : VisualElement =>
        self.Size(size, size);

    /// <summary>Sets <see cref="View.Margin"/> from horizontal and vertical values.</summary>
    public static PresetContext<T> Margin<T>(this PresetContext<T> self, double horizontal, double vertical) where T : View =>
        self.Set(View.MarginProperty, new Thickness(horizontal, vertical));

    /// <summary>Sets <see cref="View.Margin"/> from left, top, right and bottom values.</summary>
    public static PresetContext<T> Margin<T>(this PresetContext<T> self, double left, double top, double right, double bottom) where T : View =>
        self.Set(View.MarginProperty, new Thickness(left, top, right, bottom));

    /// <summary>Centers horizontally and vertically.</summary>
    public static PresetContext<T> Center<T>(this PresetContext<T> self) where T : View =>
        self.Set(View.HorizontalOptionsProperty, LayoutOptions.Center).Set(View.VerticalOptionsProperty, LayoutOptions.Center);

    /// <summary>Fills both directions.</summary>
    public static PresetContext<T> FillBoth<T>(this PresetContext<T> self) where T : View =>
        self.Set(View.HorizontalOptionsProperty, LayoutOptions.Fill).Set(View.VerticalOptionsProperty, LayoutOptions.Fill);

    /// <summary>Font size, bold/italic and family; arguments left at their defaults are not set.</summary>
    public static PresetContext<T> Font<T>(this PresetContext<T> self, double? size = null, bool bold = false, bool italic = false, string? family = null)
        where T : BindableObject, Microsoft.Maui.Controls.Internals.IFontElement
    {
        if (size is { } s)
        {
            self.Set(Label.FontSizeProperty, s);
        }

        if (bold || italic)
        {
            self.Set(Label.FontAttributesProperty,
                (bold ? FontAttributes.Bold : FontAttributes.None) | (italic ? FontAttributes.Italic : FontAttributes.None));
        }

        if (family is not null)
        {
            self.Set(Label.FontFamilyProperty, family);
        }

        return self;
    }
}

using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

/// <summary>
/// Hand-written shorthands for size, margin, alignment and font. They set the exact-name
/// properties (WidthRequest, HeightRequest, Margin, HorizontalOptions, VerticalOptions, Font*).
/// </summary>
public static class LayoutSugar
{
    /// <summary>Sets <see cref="VisualElement.HeightRequest"/>.</summary>
    public static T Height<T>(this T self, double value) where T : VisualElement
    {
        self.HeightRequest = value;
        return self;
    }

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/>.</summary>
    public static T Width<T>(this T self, double value) where T : VisualElement
    {
        self.WidthRequest = value;
        return self;
    }

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/>.</summary>
    public static T Size<T>(this T self, double width, double height) where T : VisualElement
    {
        self.WidthRequest = width;
        self.HeightRequest = height;
        return self;
    }

    /// <summary>Sets <see cref="VisualElement.WidthRequest"/> and <see cref="VisualElement.HeightRequest"/> to one value.</summary>
    public static T Size<T>(this T self, double size) where T : VisualElement => self.Size(size, size);

    /// <summary>Sets <see cref="View.Margin"/> from horizontal and vertical values.</summary>
    public static T Margin<T>(this T self, double horizontal, double vertical) where T : View
    {
        self.Margin = new Thickness(horizontal, vertical);
        return self;
    }

    /// <summary>Sets <see cref="View.Margin"/> from left, top, right and bottom values.</summary>
    public static T Margin<T>(this T self, double left, double top, double right, double bottom) where T : View
    {
        self.Margin = new Thickness(left, top, right, bottom);
        return self;
    }

    /// <summary>Centers horizontally and vertically.</summary>
    public static T Center<T>(this T self) where T : View => self.CenterHorizontal().CenterVertical();

    /// <summary>Sets <see cref="View.HorizontalOptions"/> to <see cref="LayoutOptions.Center"/>.</summary>
    public static T CenterHorizontal<T>(this T self) where T : View => self.Horizontal(LayoutOptions.Center);

    /// <summary>Sets <see cref="View.VerticalOptions"/> to <see cref="LayoutOptions.Center"/>.</summary>
    public static T CenterVertical<T>(this T self) where T : View => self.Vertical(LayoutOptions.Center);

    /// <summary>Sets <see cref="View.HorizontalOptions"/> to <see cref="LayoutOptions.Start"/>.</summary>
    public static T Start<T>(this T self) where T : View => self.Horizontal(LayoutOptions.Start);

    /// <summary>Sets <see cref="View.HorizontalOptions"/> to <see cref="LayoutOptions.End"/>.</summary>
    public static T End<T>(this T self) where T : View => self.Horizontal(LayoutOptions.End);

    /// <summary>Sets <see cref="View.VerticalOptions"/> to <see cref="LayoutOptions.Start"/>.</summary>
    public static T Top<T>(this T self) where T : View => self.Vertical(LayoutOptions.Start);

    /// <summary>Sets <see cref="View.VerticalOptions"/> to <see cref="LayoutOptions.End"/>.</summary>
    public static T Bottom<T>(this T self) where T : View => self.Vertical(LayoutOptions.End);

    /// <summary>
    /// Fills both directions. Not named <c>Fill</c>: the generated <c>Shape.Fill(Brush)</c> already
    /// uses that name on a View subtype.
    /// </summary>
    public static T FillBoth<T>(this T self) where T : View => self.FillHorizontal().FillVertical();

    /// <summary>Sets <see cref="View.HorizontalOptions"/> to <see cref="LayoutOptions.Fill"/>.</summary>
    public static T FillHorizontal<T>(this T self) where T : View => self.Horizontal(LayoutOptions.Fill);

    /// <summary>Sets <see cref="View.VerticalOptions"/> to <see cref="LayoutOptions.Fill"/>.</summary>
    public static T FillVertical<T>(this T self) where T : View => self.Vertical(LayoutOptions.Fill);

    /// <summary>
    /// Sets font size, bold/italic and family on a type with font properties (Label, Button, Entry,
    /// Editor, Span, ...). Arguments left at their defaults are not touched.
    /// </summary>
    public static T Font<T>(this T self, double? size = null, bool bold = false, bool italic = false, string? family = null)
        where T : BindableObject, Microsoft.Maui.Controls.Internals.IFontElement
    {
        // Label.FontSizeProperty, Button.FontSizeProperty, ... all alias the shared FontElement
        // properties, so the Label fields work for every IFontElement.
        if (size is { } s)
        {
            self.SetValue(Label.FontSizeProperty, s);
        }

        if (bold || italic)
        {
            self.SetValue(Label.FontAttributesProperty,
                (bold ? FontAttributes.Bold : FontAttributes.None) | (italic ? FontAttributes.Italic : FontAttributes.None));
        }

        if (family is not null)
        {
            self.SetValue(Label.FontFamilyProperty, family);
        }

        return self;
    }

    private static T Horizontal<T>(this T self, LayoutOptions options) where T : View
    {
        self.HorizontalOptions = options;
        return self;
    }

    private static T Vertical<T>(this T self, LayoutOptions options) where T : View
    {
        self.VerticalOptions = options;
        return self;
    }
}

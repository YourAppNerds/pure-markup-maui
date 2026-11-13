using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace PureMarkup.Maui;

// Padding(horizontal, vertical) and Padding(left, top, right, bottom) for every type whose
// generated layer has Padding(Thickness). One class per type: constraints are not part of a
// method signature, so the overloads cannot share a class.

/// <summary>Padding shorthands for <see cref="Border"/>.</summary>
public static class BorderPaddingSugar
{
    /// <summary>Sets <see cref="Border.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : Border
    {
        self.SetValue(Border.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="Border.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : Border
    {
        self.SetValue(Border.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="Button"/>.</summary>
public static class ButtonPaddingSugar
{
    /// <summary>Sets <see cref="Button.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : Button
    {
        self.SetValue(Button.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="Button.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : Button
    {
        self.SetValue(Button.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="ContentPresenter"/>.</summary>
public static class ContentPresenterPaddingSugar
{
    /// <summary>Sets <see cref="ContentPresenter.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : ContentPresenter
    {
        self.SetValue(ContentPresenter.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="ContentPresenter.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : ContentPresenter
    {
        self.SetValue(ContentPresenter.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="ImageButton"/>.</summary>
public static class ImageButtonPaddingSugar
{
    /// <summary>Sets <see cref="ImageButton.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : ImageButton
    {
        self.SetValue(ImageButton.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="ImageButton.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : ImageButton
    {
        self.SetValue(ImageButton.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="Label"/>.</summary>
public static class LabelPaddingSugar
{
    /// <summary>Sets <see cref="Label.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : Label
    {
        self.SetValue(Label.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="Label.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : Label
    {
        self.SetValue(Label.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="Layout"/>.</summary>
public static class LayoutPaddingSugar
{
    /// <summary>Sets <see cref="Layout.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : Layout
    {
        self.SetValue(Layout.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="Layout.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : Layout
    {
        self.SetValue(Layout.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="Page"/>.</summary>
public static class PagePaddingSugar
{
    /// <summary>Sets <see cref="Page.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : Page
    {
        self.SetValue(Page.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="Page.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : Page
    {
        self.SetValue(Page.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="ScrollView"/>.</summary>
public static class ScrollViewPaddingSugar
{
    /// <summary>Sets <see cref="ScrollView.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : ScrollView
    {
        self.SetValue(ScrollView.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="ScrollView.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : ScrollView
    {
        self.SetValue(ScrollView.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

/// <summary>Padding shorthands for <see cref="TemplatedView"/>.</summary>
public static class TemplatedViewPaddingSugar
{
    /// <summary>Sets <see cref="TemplatedView.Padding"/> from horizontal and vertical values.</summary>
    public static T Padding<T>(this T self, double horizontal, double vertical) where T : TemplatedView
    {
        self.SetValue(TemplatedView.PaddingProperty, new Thickness(horizontal, vertical));
        return self;
    }

    /// <summary>Sets <see cref="TemplatedView.Padding"/> from left, top, right and bottom values.</summary>
    public static T Padding<T>(this T self, double left, double top, double right, double bottom) where T : TemplatedView
    {
        self.SetValue(TemplatedView.PaddingProperty, new Thickness(left, top, right, bottom));
        return self;
    }
}

using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using PureMarkup.Maui.Defaults;
using C = PureMarkup.Maui.Defaults.DefaultColors;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>The template look (Styles.xaml + Colors.xaml) ported to <see cref="DefaultLook"/>, headless.</summary>
[Collection(nameof(PresetTests))] // Theme and the preset cache are static
public class DefaultLookTests
{
    public static TheoryData<AppTheme> Themes => [AppTheme.Light, AppTheme.Dark];

    private static ContentPage Host(AppTheme theme, params View[] views)
    {
        var page = new ContentPage();
        Theme.Apply(page.Resources, theme);
        page.Resources.UseDefaultLook();
        var stack = new VerticalStackLayout();
        foreach (var view in views)
        {
            stack.Add(view);
        }

        page.Content = stack;
        return page;
    }

    private static Color Pick(AppTheme theme, Color light, Color dark) => theme == AppTheme.Dark ? dark : light;

    [Theory, MemberData(nameof(Themes))]
    public void TextControls(AppTheme theme)
    {
        var label = new Label();
        var entry = new Entry();
        var editor = new Editor();
        var radio = new RadioButton();
        var page = Host(theme, label, entry, editor, radio);

        Assert.Equal(Pick(theme, C.White, C.OffBlack), page.BackgroundColor);
        Assert.Equal(new Thickness(0), page.Padding);
        foreach (var color in new[] { label.TextColor, entry.TextColor, editor.TextColor, radio.TextColor })
        {
            Assert.Equal(Pick(theme, C.Black, C.White), color);
        }

        Assert.Equal((14d, "OpenSansRegular"), (label.FontSize, label.FontFamily));
        Assert.Equal(Colors.Transparent, entry.BackgroundColor);
        Assert.Equal(Pick(theme, C.Gray200, C.Gray500), entry.PlaceholderColor);
        Assert.Equal(Pick(theme, C.Gray200, C.Gray500), editor.PlaceholderColor);
        Assert.Equal((44d, 44d), (entry.MinimumHeightRequest, entry.MinimumWidthRequest));

        entry.IsEnabled = label.IsEnabled = false;
        Assert.Equal(Pick(theme, C.Gray300, C.Gray600), entry.TextColor);
        Assert.Equal(Pick(theme, C.Gray300, C.Gray600), label.TextColor);
        entry.IsEnabled = true;
        Assert.Equal(Pick(theme, C.Black, C.White), entry.TextColor);
    }

    [Theory, MemberData(nameof(Themes))]
    public void ButtonAndImageButton(AppTheme theme)
    {
        var button = new Button();
        var image = new ImageButton();
        Host(theme, button, image);

        Assert.Equal(Pick(theme, C.Primary, C.PrimaryDark), button.BackgroundColor);
        Assert.Equal(Pick(theme, C.White, C.PrimaryDarkText), button.TextColor);
        Assert.Equal((8, 0d, 14d), (button.CornerRadius, button.BorderWidth, button.FontSize));
        Assert.Equal(new Thickness(14, 10), button.Padding);

        button.IsEnabled = false;
        Assert.Equal(Pick(theme, C.Gray200, C.Gray600), button.BackgroundColor);
        Assert.Equal(Pick(theme, C.Gray950, C.Gray200), button.TextColor);
        button.IsEnabled = true;
        Assert.Equal(Pick(theme, C.Primary, C.PrimaryDark), button.BackgroundColor);

        Assert.Equal((0, 0d), (image.CornerRadius, image.BorderWidth));
        image.IsEnabled = false;
        Assert.Equal(0.5, image.Opacity);
        image.IsEnabled = true;
        Assert.Equal(1, image.Opacity);
    }

    [Theory, MemberData(nameof(Themes))]
    public void Pickers(AppTheme theme)
    {
        var picker = new Picker();
        var date = new DatePicker();
        var time = new TimePicker();
        var search = new SearchBar();
        Host(theme, picker, date, time, search);

        foreach (var color in new[] { picker.TextColor, date.TextColor, time.TextColor, search.TextColor })
        {
            Assert.Equal(Pick(theme, C.Gray900, C.White), color);
        }

        Assert.Equal(Pick(theme, C.Gray900, C.Gray200), picker.TitleColor);
        Assert.Equal(C.Gray500, search.PlaceholderColor);
        Assert.Equal(C.Gray500, search.CancelButtonColor);

        picker.IsEnabled = date.IsEnabled = time.IsEnabled = search.IsEnabled = false;
        Assert.Equal(Pick(theme, C.Gray300, C.Gray600), picker.TitleColor);
        Assert.Equal(Pick(theme, C.Gray200, C.Gray500), date.TextColor); // DatePicker uses the muted pair
        Assert.Equal(Pick(theme, C.Gray300, C.Gray600), time.TextColor);
        Assert.Equal(Pick(theme, C.Gray300, C.Gray600), search.PlaceholderColor);
    }

    [Theory, MemberData(nameof(Themes))]
    public void AccentControls(AppTheme theme)
    {
        var activity = new ActivityIndicator();
        var check = new CheckBox();
        var progress = new ProgressBar();
        var slider = new Slider();
        var toggle = new Switch();
        Host(theme, activity, check, progress, slider, toggle);

        var accent = Pick(theme, C.Primary, C.White);
        Assert.Equal(accent, activity.Color);
        Assert.Equal(accent, check.Color);
        Assert.Equal(accent, progress.ProgressColor);
        Assert.Equal((accent, accent), (slider.MinimumTrackColor, slider.ThumbColor));
        Assert.Equal(Pick(theme, C.Gray200, C.Gray600), slider.MaximumTrackColor);

        // Switch starts Off: the Off state sets the thumb.
        Assert.Equal(Pick(theme, C.Gray400, C.Gray500), toggle.ThumbColor);
        toggle.IsToggled = true;
        Assert.Equal(Pick(theme, C.Secondary, C.Gray200), toggle.OnColor);
        Assert.Equal(accent, toggle.ThumbColor);

        check.IsEnabled = slider.IsEnabled = progress.IsEnabled = false;
        var disabled = Pick(theme, C.Gray300, C.Gray600);
        Assert.Equal(disabled, check.Color);
        Assert.Equal(disabled, progress.ProgressColor);
        Assert.Equal((disabled, disabled, disabled), (slider.MinimumTrackColor, slider.MaximumTrackColor, slider.ThumbColor));
    }

    [Theory, MemberData(nameof(Themes))]
    public void Containers(AppTheme theme)
    {
        var border = new Border();
        var box = new BoxView();
        var indicator = new IndicatorView();
        var refresh = new RefreshView();
        Host(theme, border, box, indicator, refresh);

        Assert.Equal(Pick(theme, C.Gray200, C.Gray500), ((SolidColorBrush)border.Stroke).Color);
        Assert.IsType<Rectangle>(border.StrokeShape);
        Assert.Equal(1, border.StrokeThickness);
        Assert.Equal(Pick(theme, C.Gray950, C.Gray200), box.BackgroundColor);
        Assert.Equal(Pick(theme, C.Gray200, C.Gray500), indicator.IndicatorColor);
        Assert.Equal(Pick(theme, C.Gray950, C.Gray100), indicator.SelectedIndicatorColor);
        Assert.Equal(Pick(theme, C.Gray900, C.Gray200), refresh.RefreshColor);
    }

    [Theory, MemberData(nameof(Themes))]
    public void Pages(AppTheme theme)
    {
        var nav = new NavigationPage();
        Theme.Apply(nav.Resources, theme);
        nav.Resources.UseDefaultLook();
        Assert.Equal(Pick(theme, C.White, C.OffBlack), nav.BarBackgroundColor);
        Assert.Equal(Pick(theme, C.Gray200, C.White), nav.BarTextColor);

        // TabbedPage and Shell need a dispatcher to construct; check their Styles' setters instead.
        var tabs = DefaultLook.TabbedPage.Style;
        Assert.Equal(Pick(theme, C.White, C.Gray950), Value(tabs, TabbedPage.BarBackgroundColorProperty, theme));
        Assert.Equal(Pick(theme, C.Magenta, C.White), Value(tabs, TabbedPage.BarTextColorProperty, theme));
        Assert.Equal(Pick(theme, C.Gray950, C.Gray200), Value(tabs, TabbedPage.SelectedTabColorProperty, theme));

        var shell = DefaultLook.Shell.Style;
        Assert.Equal(Pick(theme, C.White, C.OffBlack), Value(shell, Shell.BackgroundColorProperty, theme));
        Assert.Equal(Pick(theme, C.Magenta, C.White), Value(shell, Shell.TabBarForegroundColorProperty, theme));
        Assert.Equal(false, Value(shell, Shell.NavBarHasShadowProperty, theme));
    }

    private static object? Value(Style style, BindableProperty property, AppTheme theme)
    {
        var value = System.Linq.Enumerable.Single(style.Setters, s => s.Property == property).Value;
        if (value is Microsoft.Maui.Controls.Internals.DynamicResource dynamic)
        {
            var resources = new ResourceDictionary();
            Theme.Apply(resources, theme);
            return resources[dynamic.Key];
        }

        return value;
    }

    [Fact]
    public void ThemeSwitchUpdatesLive()
    {
        var button = new Button();
        var page = Host(AppTheme.Light, button);
        Assert.Equal(C.Primary, button.BackgroundColor);
        Theme.Apply(page.Resources, AppTheme.Dark);
        Assert.Equal(C.PrimaryDark, button.BackgroundColor);
        Assert.Equal(C.OffBlack, page.BackgroundColor);
    }

    [Fact]
    public void LocalValuesAndUseBeatDefaults()
    {
        var danger = DefaultLook.Button.With(static p => p.BackgroundColor(Colors.Red));
        var local = new Button().BackgroundColor(Colors.Green).FontSize(20);
        var used = new Button().Use(danger);
        var headline = new Label().Use(DefaultLook.Headline);
        Host(AppTheme.Light, local, used, headline);

        Assert.Equal((Colors.Green, 20d), (local.BackgroundColor, local.FontSize));
        Assert.Equal(8, local.CornerRadius);                 // untouched default still applies
        Assert.Equal(Colors.Red, used.BackgroundColor);      // .With beats its base
        Assert.Equal(C.White, used.TextColor);
        Assert.Equal(32, headline.FontSize);                 // explicit Headline beats implicit Label
        Assert.Equal(C.MidnightBlue, headline.TextColor);
        Assert.Equal("OpenSansRegular", headline.FontFamily); // implicit Label setters still underneath

        // MAUI: setters of an implicit style's visual states rank below local values (unlike an
        // explicit Style's), so a local background survives Disabled, as with the XAML template.
        local.IsEnabled = false;
        Assert.Equal(Colors.Green, local.BackgroundColor);
    }

    [Fact]
    public void NullFontKeepsPlatformFont()
    {
        var page = new ContentPage();
        page.Resources.UseDefaultLook(fontFamily: null);
        var label = new Label();
        page.Content = label;
        Assert.Null(label.FontFamily);
    }
}

using Microsoft.Maui.Graphics;

namespace PureMarkup.Maui.Defaults;

/// <summary>
/// The palette of the .NET MAUI app template (<c>Resources/Styles/Colors.xaml</c> in
/// <c>maui-mobile</c>, without the sample-content colors).
/// </summary>
public static class DefaultColors
{
    /// <summary>#512BD4.</summary>
    public static readonly Color Primary = Color.FromArgb("#512BD4");

    /// <summary>#AC99EA.</summary>
    public static readonly Color PrimaryDark = Color.FromArgb("#AC99EA");

    /// <summary>#242424.</summary>
    public static readonly Color PrimaryDarkText = Color.FromArgb("#242424");

    /// <summary>#DFD8F7.</summary>
    public static readonly Color Secondary = Color.FromArgb("#DFD8F7");

    /// <summary>#9880E5.</summary>
    public static readonly Color SecondaryDarkText = Color.FromArgb("#9880E5");

    /// <summary>#2B0B98.</summary>
    public static readonly Color Tertiary = Color.FromArgb("#2B0B98");

    /// <summary>White.</summary>
    public static readonly Color White = Colors.White;

    /// <summary>Black.</summary>
    public static readonly Color Black = Colors.Black;

    /// <summary>#D600AA.</summary>
    public static readonly Color Magenta = Color.FromArgb("#D600AA");

    /// <summary>#190649.</summary>
    public static readonly Color MidnightBlue = Color.FromArgb("#190649");

    /// <summary>#1F1F1F.</summary>
    public static readonly Color OffBlack = Color.FromArgb("#1F1F1F");

    /// <summary>#E1E1E1.</summary>
    public static readonly Color Gray100 = Color.FromArgb("#E1E1E1");

    /// <summary>#C8C8C8.</summary>
    public static readonly Color Gray200 = Color.FromArgb("#C8C8C8");

    /// <summary>#ACACAC.</summary>
    public static readonly Color Gray300 = Color.FromArgb("#ACACAC");

    /// <summary>#919191.</summary>
    public static readonly Color Gray400 = Color.FromArgb("#919191");

    /// <summary>#6E6E6E.</summary>
    public static readonly Color Gray500 = Color.FromArgb("#6E6E6E");

    /// <summary>#404040.</summary>
    public static readonly Color Gray600 = Color.FromArgb("#404040");

    /// <summary>#212121.</summary>
    public static readonly Color Gray900 = Color.FromArgb("#212121");

    /// <summary>#141414.</summary>
    public static readonly Color Gray950 = Color.FromArgb("#141414");
}

/// <summary>
/// The light/dark pairs the template's <c>Styles.xaml</c> uses as <c>AppThemeBinding</c>s, as
/// <see cref="Token{TValue}"/>s. Reuse them in your own presets to match the default look.
/// </summary>
public static class DefaultTokens
{
    /// <summary>Black / White: Label, Entry, Editor, RadioButton text.</summary>
    public static readonly Token<Color> Text = new(DefaultColors.Black, DefaultColors.White, "Default.Text");

    /// <summary>Gray900 / White: Picker, DatePicker, TimePicker, SearchBar text.</summary>
    public static readonly Token<Color> PickerText = new(DefaultColors.Gray900, DefaultColors.White, "Default.PickerText");

    /// <summary>Gray300 / Gray600: text and accents of disabled controls.</summary>
    public static readonly Token<Color> Disabled = new(DefaultColors.Gray300, DefaultColors.Gray600, "Default.Disabled");

    /// <summary>Gray200 / Gray500: placeholders, Border stroke, IndicatorView dots, disabled DatePicker text.</summary>
    public static readonly Token<Color> Muted = new(DefaultColors.Gray200, DefaultColors.Gray500, "Default.Muted");

    /// <summary>Primary / White: ActivityIndicator, CheckBox, ProgressBar, Slider, Switch.</summary>
    public static readonly Token<Color> Accent = new(DefaultColors.Primary, DefaultColors.White, "Default.Accent");

    /// <summary>Primary / PrimaryDark: Button background.</summary>
    public static readonly Token<Color> ButtonBackground = new(DefaultColors.Primary, DefaultColors.PrimaryDark, "Default.ButtonBackground");

    /// <summary>White / PrimaryDarkText: Button text.</summary>
    public static readonly Token<Color> ButtonText = new(DefaultColors.White, DefaultColors.PrimaryDarkText, "Default.ButtonText");

    /// <summary>Gray200 / Gray600: disabled Button background, Slider maximum track.</summary>
    public static readonly Token<Color> Track = new(DefaultColors.Gray200, DefaultColors.Gray600, "Default.Track");

    /// <summary>Gray950 / Gray200: disabled Button text, BoxView, TabbedPage selected tab.</summary>
    public static readonly Token<Color> Strong = new(DefaultColors.Gray950, DefaultColors.Gray200, "Default.Strong");

    /// <summary>Gray950 / Gray100: IndicatorView selected dot.</summary>
    public static readonly Token<Color> SelectedIndicator = new(DefaultColors.Gray950, DefaultColors.Gray100, "Default.SelectedIndicator");

    /// <summary>Gray900 / Gray200: Picker title, RefreshView spinner, Shell unselected tab.</summary>
    public static readonly Token<Color> Secondary = new(DefaultColors.Gray900, DefaultColors.Gray200, "Default.Secondary");

    /// <summary>Secondary / Gray200: Switch track when on.</summary>
    public static readonly Token<Color> SwitchOnTrack = new(DefaultColors.Secondary, DefaultColors.Gray200, "Default.SwitchOnTrack");

    /// <summary>Gray400 / Gray500: Switch thumb when off.</summary>
    public static readonly Token<Color> SwitchOffThumb = new(DefaultColors.Gray400, DefaultColors.Gray500, "Default.SwitchOffThumb");

    /// <summary>White / OffBlack: page, Shell and NavigationPage bar background.</summary>
    public static readonly Token<Color> PageBackground = new(DefaultColors.White, DefaultColors.OffBlack, "Default.PageBackground");

    /// <summary>Black / SecondaryDarkText: Shell foreground and title.</summary>
    public static readonly Token<Color> ShellForeground = new(DefaultColors.Black, DefaultColors.SecondaryDarkText, "Default.ShellForeground");

    /// <summary>Gray200 / Gray950: Shell disabled, TabbedPage unselected tab.</summary>
    public static readonly Token<Color> Inactive = new(DefaultColors.Gray200, DefaultColors.Gray950, "Default.Inactive");

    /// <summary>White / Black: Shell tab bar background.</summary>
    public static readonly Token<Color> TabBarBackground = new(DefaultColors.White, DefaultColors.Black, "Default.TabBarBackground");

    /// <summary>Magenta / White: Shell tab bar foreground and title, TabbedPage bar text.</summary>
    public static readonly Token<Color> TabBarForeground = new(DefaultColors.Magenta, DefaultColors.White, "Default.TabBarForeground");

    /// <summary>Gray200 / White: NavigationPage bar text and icon.</summary>
    public static readonly Token<Color> NavigationBarText = new(DefaultColors.Gray200, DefaultColors.White, "Default.NavigationBarText");

    /// <summary>White / Gray950: TabbedPage bar background.</summary>
    public static readonly Token<Color> TabbedBarBackground = new(DefaultColors.White, DefaultColors.Gray950, "Default.TabbedBarBackground");

    /// <summary>MidnightBlue / White: Headline and SubHeadline text.</summary>
    public static readonly Token<Color> Headline = new(DefaultColors.MidnightBlue, DefaultColors.White, "Default.Headline");
}

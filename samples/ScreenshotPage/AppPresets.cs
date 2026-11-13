using PureMarkup.Maui;

namespace ScreenshotPage;

/// <summary>Theme vocabulary: one value per light/dark theme, live on theme change.</summary>
public static class Tokens
{
    public static readonly Token<Color> Page = new(Colors.White, Color.FromArgb("#1E1E1E"), nameof(Page));
    public static readonly Token<Color> Text = new(Colors.Black, Colors.WhiteSmoke, nameof(Text));
    public static readonly Token<Color> Surface = new(Colors.AliceBlue, Color.FromArgb("#2D2D30"), nameof(Surface));
    public static readonly Token<Color> Accent = new(Color.FromArgb("#512BD4"), Color.FromArgb("#9F86FF"), nameof(Accent));
    public static readonly Token<Color> Danger = new(Color.FromArgb("#C62828"), Color.FromArgb("#EF5350"), nameof(Danger));
    public static readonly Token<double> Body = new(15, nameof(Body));
}

/// <summary>The app look. Entry and Button are registered as implicit defaults in App.</summary>
public static class AppPresets
{
    public static Preset<Entry> Entry => Preset.Once<Entry>(static p => p
        .FontSize(Tokens.Body).Height(44).Margin(5, 5)
        .TextColor(Tokens.Text).BackgroundColor(Tokens.Surface)
        .State(VisualStateManager.CommonStates.Disabled, static s => s.Opacity(0.5)));

    public static Preset<Button> Button => Preset.Once<Button>(static p => p
        .FontSize(Tokens.Body).Margin(5, 5).CornerRadius(6)
        .TextColor(Colors.White).BackgroundColor(Tokens.Accent));

    public static Preset<Button> DangerButton => Button.With(static p => p
        .BackgroundColor(Tokens.Danger).Font(bold: true));
}

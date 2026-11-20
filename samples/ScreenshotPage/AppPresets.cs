using PureMarkup.Maui;
using PureMarkup.Maui.Defaults;

namespace ScreenshotPage;

/// <summary>Theme vocabulary on top of the template's DefaultTokens.</summary>
public static class Tokens
{
    public static readonly Token<Color> Danger = new(Color.FromArgb("#C62828"), Color.FromArgb("#EF5350"), nameof(Danger));
}

/// <summary>
/// The app look: the MAUI template's (DefaultLook, registered in App) plus a danger button derived
/// from the default Button.
/// </summary>
public static class AppPresets
{
    public static Preset<Button> DangerButton => DefaultLook.Button.With(static p => p
        .BackgroundColor(Tokens.Danger).TextColor(Colors.White).Font(bold: true));
}

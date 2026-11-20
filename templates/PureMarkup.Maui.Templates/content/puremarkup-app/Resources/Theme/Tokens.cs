namespace PureMarkupApp._1.Resources.Theme;

/// <summary>
/// The app's design tokens. The stock template's colors are in <see cref="DefaultTokens"/>
/// (light and dark values); add your own here. A Token&lt;T&gt; works on elements and in presets
/// and follows the app theme live.
/// </summary>
public static class Tokens
{
	public static readonly Token<Color> Danger = new(Color.FromArgb("#C62828"), Color.FromArgb("#EF5350"), nameof(Danger));

	public static readonly Token<Color> Caption = new(DefaultColors.Gray500, DefaultColors.Gray400, nameof(Caption));

	public const double CaptionSize = 12;
}

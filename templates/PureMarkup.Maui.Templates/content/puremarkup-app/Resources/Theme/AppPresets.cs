namespace PureMarkupApp._1.Resources.Theme;

/// <summary>
/// Named looks on top of the template's (<see cref="DefaultLook"/>, registered in App).
/// <c>.With(...)</c> derives a preset (a MAUI Style with BasedOn): use it with
/// <c>new Button().Use(AppPresets.PrimaryButton)</c> or <c>AppPresets.PrimaryButton.New()</c>.
/// </summary>
public static class AppPresets
{
	public static Preset<Button> PrimaryButton => DefaultLook.Button.With(static p => p
		.Font(bold: true));

	public static Preset<Button> DangerButton => DefaultLook.Button.With(static p => p
		.BackgroundColor(Tokens.Danger).TextColor(Colors.White));

	public static Preset<Label> Caption => DefaultLook.Label.With(static p => p
		.FontSize(Tokens.CaptionSize).TextColor(Tokens.Caption));
}

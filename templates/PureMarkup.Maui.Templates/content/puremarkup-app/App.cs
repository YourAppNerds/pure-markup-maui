namespace PureMarkupApp._1;

public class App : Application
{
	// The look of the stock MAUI template (its Styles.xaml and Colors.xaml) as implicit presets.
	// Register your own defaults after it: r.Default(AppPresets.X) replaces that type's default.
	public App() =>
		this.UsePresets(r => r.UseDefaultLook());

	protected override Window CreateWindow(IActivationState? activationState) =>
		new(new AppShell());
}

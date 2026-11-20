using PureMarkup.Maui;
using PureMarkup.Maui.Defaults;

namespace ScreenshotPage;

public class App : Application
{
    // The MAUI template's Styles.xaml look, as implicit default presets.
    public App() =>
        this.UsePresets(r => r.UseDefaultLook());

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage()) { Title = "PureMarkup.Maui sample", Width = 640, Height = 480 };
}

using PureMarkup.Maui;

namespace ScreenshotPage;

public class App : Application
{
    public App() =>
        this.UsePresets(r => r.Default(AppPresets.Entry).Default(AppPresets.Button));

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage()) { Title = "PureMarkup.Maui sample", Width = 640, Height = 480 };
}

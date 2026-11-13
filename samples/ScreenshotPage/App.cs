namespace ScreenshotPage;

public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage()) { Title = "PureMarkup.Maui sample", Width = 640, Height = 420 };
}

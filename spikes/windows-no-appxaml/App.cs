namespace WindowsNoAppXaml;

public class App : Application
{
    // SPIKE_SHELL=1 hosts the page in a Shell with two flyout items, to exercise MAUI's
    // XAML-defined WinUI templates (RootNavigationView), which need XAML type metadata.
    protected override Window CreateWindow(IActivationState? activationState)
    {
        Page root = Environment.GetEnvironmentVariable("SPIKE_SHELL") == "1"
            ? new Shell
            {
                FlyoutBehavior = FlyoutBehavior.Locked,
                Items =
                {
                    new ShellContent { Title = "Main", ContentTemplate = new DataTemplate(typeof(MainPage)) },
                    new ShellContent { Title = "Second", Content = new ContentPage { Content = new Label { Text = "Second page" } } },
                },
            }
            : new MainPage();
        return new Window(root) { Title = "No App.xaml spike" };
    }
}

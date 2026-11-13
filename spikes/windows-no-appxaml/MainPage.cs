namespace WindowsNoAppXaml;

public class MainPage : ContentPage
{
    int _count;

    public MainPage()
    {
        var label = new Label { Text = "Hello from C#-only MAUI on WinUI", FontSize = 24 };
        var entry = new Entry { Placeholder = "Type here", AutomationId = "SpikeEntry" };
        var button = new Button { Text = "Click me" };
        button.Clicked += (_, _) => button.Text = $"Clicked {++_count} time(s): {entry.Text}";

        Content = new VerticalStackLayout
        {
            Padding = 30,
            Spacing = 20,
            Children = { label, entry, button },
        };

        Loaded += (_, _) => entry.Focus();
    }
}

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PageConstruction.Bench;

/// <summary>Plain setters for layout and text; looks come from shared, prebuilt MAUI Styles.</summary>
internal static class PlainStylesPage
{
    static readonly Style FormLabel = new(typeof(Label))
    {
        ApplyToDerivedTypes = true,
        Setters = { new Setter { Property = Label.VerticalTextAlignmentProperty, Value = TextAlignment.Center } },
    };

    static readonly Style Field = new(typeof(Entry))
    {
        ApplyToDerivedTypes = true,
        Setters =
        {
            new Setter { Property = Entry.FontSizeProperty, Value = 15d },
            new Setter { Property = Entry.TextColorProperty, Value = Colors.Black },
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Colors.White },
            new Setter { Property = VisualElement.HeightRequestProperty, Value = 44d },
            new Setter { Property = View.MarginProperty, Value = new Thickness(5, 5) },
        },
    };

    static readonly Style PrimaryButton = new(typeof(Button))
    {
        ApplyToDerivedTypes = true,
        Setters =
        {
            new Setter { Property = VisualElement.BackgroundColorProperty, Value = Form.Primary },
            new Setter { Property = Button.TextColorProperty, Value = Colors.White },
            new Setter { Property = Button.CornerRadiusProperty, Value = 8 },
            new Setter { Property = Button.FontSizeProperty, Value = 14d },
            new Setter { Property = VisualElement.HeightRequestProperty, Value = 44d },
        },
    };

    static readonly Style DangerButton = new(typeof(Button))
    {
        ApplyToDerivedTypes = true,
        BasedOn = PrimaryButton,
        Setters = { new Setter { Property = VisualElement.BackgroundColorProperty, Value = Form.Danger } },
    };

    public static ContentPage Build()
    {
        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
        };

        var title = new Label { Text = "Registration", FontSize = 22, FontAttributes = FontAttributes.Bold };
        Grid.SetRow(title, 0);
        Grid.SetColumnSpan(title, 2);
        grid.Add(title);

        for (var i = 0; i < 4; i++)
        {
            var label = new Label { Text = Form.Labels[i], Style = FormLabel };
            Grid.SetRow(label, i + 1);
            grid.Add(label);

            var entry = new Entry { Placeholder = Form.Placeholders[i], Keyboard = Form.Keyboards[i], Style = Field };
            Grid.SetRow(entry, i + 1);
            Grid.SetColumn(entry, 1);
            grid.Add(entry);
        }

        var register = new Button { Text = "Register", Style = PrimaryButton };
        register.Clicked += Form.OnRegister;
        Grid.SetRow(register, 5);
        Grid.SetColumn(register, 1);
        grid.Add(register);

        var reset = new Button { Text = "Reset", Style = DangerButton };
        reset.Clicked += Form.OnReset;
        Grid.SetRow(reset, 6);
        Grid.SetColumn(reset, 1);
        grid.Add(reset);

        return new ContentPage { Padding = 24, Content = grid };
    }
}

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PageConstruction.Bench;

internal static class PlainPage
{
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
            var label = new Label { Text = Form.Labels[i], VerticalTextAlignment = TextAlignment.Center };
            Grid.SetRow(label, i + 1);
            grid.Add(label);

            var entry = new Entry
            {
                Placeholder = Form.Placeholders[i],
                Keyboard = Form.Keyboards[i],
                FontSize = 15,
                TextColor = Colors.Black,
                BackgroundColor = Colors.White,
                HeightRequest = 44,
                Margin = new Thickness(5, 5),
            };
            Grid.SetRow(entry, i + 1);
            Grid.SetColumn(entry, 1);
            grid.Add(entry);
        }

        var register = new Button
        {
            Text = "Register", BackgroundColor = Form.Primary, TextColor = Colors.White,
            CornerRadius = 8, FontSize = 14, HeightRequest = 44,
        };
        register.Clicked += Form.OnRegister;
        Grid.SetRow(register, 5);
        Grid.SetColumn(register, 1);
        grid.Add(register);

        var reset = new Button
        {
            Text = "Reset", BackgroundColor = Form.Danger, TextColor = Colors.White,
            CornerRadius = 8, FontSize = 14, HeightRequest = 44,
        };
        reset.Clicked += Form.OnReset;
        Grid.SetRow(reset, 6);
        Grid.SetColumn(reset, 1);
        grid.Add(reset);

        return new ContentPage { Padding = 24, Content = grid };
    }
}

using CommunityToolkit.Maui.Markup;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;

namespace PageConstruction.Bench;

internal static class ToolkitPage
{
    enum Row { Title, Name, Email, Phone, Code, Register, Reset }

    enum Col { Label, Input }

    public static ContentPage Build()
    {
        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 12,
            RowDefinitions = Rows.Define((Row.Title, Auto), (Row.Name, Auto), (Row.Email, Auto), (Row.Phone, Auto),
                (Row.Code, Auto), (Row.Register, Auto), (Row.Reset, Auto)),
            ColumnDefinitions = Columns.Define((Col.Label, Auto), (Col.Input, Star)),
        };

        grid.Add(new Label { Text = "Registration" }.Font(size: 22, bold: true).Row(Row.Title).ColumnSpan(2));

        for (var i = 0; i < 4; i++)
        {
            grid.Add(new Label { Text = Form.Labels[i] }.TextCenterVertical().Row(Row.Name + i).Column(Col.Label));
            grid.Add(new Entry { Placeholder = Form.Placeholders[i], Keyboard = Form.Keyboards[i] }
                .FontSize(15).TextColor(Colors.Black).BackgroundColor(Colors.White)
                .Height(44).Margin(5, 5)
                .Row(Row.Name + i).Column(Col.Input));
        }

        grid.Add(new Button { Text = "Register", CornerRadius = 8 }.BackgroundColor(Form.Primary).TextColor(Colors.White)
            .FontSize(14).Height(44).Invoke(static b => b.Clicked += Form.OnRegister)
            .Row(Row.Register).Column(Col.Input));
        grid.Add(new Button { Text = "Reset", CornerRadius = 8 }.BackgroundColor(Form.Danger).TextColor(Colors.White)
            .FontSize(14).Height(44).Invoke(static b => b.Clicked += Form.OnReset)
            .Row(Row.Reset).Column(Col.Input));

        return new ContentPage { Content = grid }.Padding(24);
    }
}

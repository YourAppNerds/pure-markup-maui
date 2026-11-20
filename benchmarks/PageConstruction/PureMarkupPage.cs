using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PureMarkup.Maui;
using static PureMarkup.Maui.GridRowsColumns;

namespace PageConstruction.Bench;

internal static class PureMarkupPage
{
    enum Row { Title, Name, Email, Phone, Code, Register, Reset }

    enum Col { Label, Input }

    public static ContentPage Build()
    {
        var grid = new Grid()
            .RowSpacing(8).ColumnSpacing(12)
            .RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Name, Auto), (Row.Email, Auto), (Row.Phone, Auto),
                (Row.Code, Auto), (Row.Register, Auto), (Row.Reset, Auto)))
            .ColumnDefinitions(Columns.Define((Col.Label, Auto), (Col.Input, Star)));

        grid.Add(new Label().Text("Registration").Font(size: 22, bold: true).Row(Row.Title).Column(Col.Label, Col.Input));

        for (var i = 0; i < 4; i++)
        {
            grid.Add(new Label().Text(Form.Labels[i]).VerticalTextAlignment(TextAlignment.Center)
                .Row(Row.Name + i).Column(Col.Label));
            grid.Add(new Entry()
                .Placeholder(Form.Placeholders[i]).Keyboard(Form.Keyboards[i])
                .FontSize(15).TextColor(Colors.Black).BackgroundColor(Colors.White)
                .Height(44).Margin(5, 5)
                .Row(Row.Name + i).Column(Col.Input));
        }

        grid.Add(new Button().Text("Register").BackgroundColor(Form.Primary).TextColor(Colors.White)
            .CornerRadius(8).FontSize(14).Height(44).OnClicked(Form.OnRegister)
            .Row(Row.Register).Column(Col.Input));
        grid.Add(new Button().Text("Reset").BackgroundColor(Form.Danger).TextColor(Colors.White)
            .CornerRadius(8).FontSize(14).Height(44).OnClicked(Form.OnReset)
            .Row(Row.Reset).Column(Col.Input));

        return new ContentPage().Padding(24).Content(grid);
    }
}

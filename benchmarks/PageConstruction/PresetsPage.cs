using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PureMarkup.Maui;
using static PureMarkup.Maui.GridRowsColumns;

namespace PageConstruction.Bench;

/// <summary>PureMarkup with the looks as presets, written the documented way (property getters).</summary>
internal static class PresetsPage
{
    enum Row { Title, Name, Email, Phone, Code, Register, Reset }

    enum Col { Label, Input }

    static Preset<Label> FormLabel => Preset.Once<Label>(static p => p.VerticalTextAlignment(TextAlignment.Center));

    static Preset<Entry> Field => Preset.Once<Entry>(static p => p
        .FontSize(15).TextColor(Colors.Black).BackgroundColor(Colors.White).Height(44).Margin(5, 5));

    static Preset<Button> PrimaryButton => Preset.Once<Button>(static p => p
        .BackgroundColor(Form.Primary).TextColor(Colors.White).CornerRadius(8).FontSize(14).Height(44));

    static Preset<Button> DangerButton => PrimaryButton.With(static p => p.BackgroundColor(Form.Danger));

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
            grid.Add(FormLabel.New().Text(Form.Labels[i]).Row(Row.Name + i).Column(Col.Label));
            grid.Add(Field.New().Placeholder(Form.Placeholders[i]).Keyboard(Form.Keyboards[i])
                .Row(Row.Name + i).Column(Col.Input));
        }

        grid.Add(PrimaryButton.New().Text("Register").OnClicked(Form.OnRegister).Row(Row.Register).Column(Col.Input));
        grid.Add(DangerButton.New().Text("Reset").OnClicked(Form.OnReset).Row(Row.Reset).Column(Col.Input));

        return new ContentPage().Padding(24).Content(grid);
    }
}

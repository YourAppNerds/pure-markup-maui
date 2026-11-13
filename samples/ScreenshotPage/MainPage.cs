using PureMarkup.Maui;
using static PureMarkup.Maui.GridRowsColumns;

namespace ScreenshotPage;

public class MainPage : ContentPage
{
    enum Row { Title, Code, Register, Status }

    enum Col { Label, Input }

    readonly ViewModel _vm = new();

    public MainPage()
    {
        this.BindingContext(_vm).Padding(24)
            .Content(new Grid
            {
                Children =
                {
                    new Label().Text("Registration").Font(size: 22, bold: true)
                        .Row(Row.Title).Column(Col.Label, Col.Input),

                    new Label().Text("Code").FontSize(15).VerticalTextAlignment(TextAlignment.Center)
                        .Row(Row.Code).Column(Col.Label),

                    new Entry()
                        .Keyboard(Keyboard.Numeric).BackgroundColor(Colors.AliceBlue)
                        .Row(Row.Code).Column(Col.Input)
                        .FontSize(15).Placeholder("Enter number").TextColor(Colors.Black)
                        .Height(44).Margin(5, 5)
                        .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay))
                        .AutomationId("CodeEntry"),

                    new Button().Text("Register").Row(Row.Register).Column(Col.Input).Margin(5, 5)
                        .OnClicked(_vm.Register)
                        .AutomationId("RegisterButton"),

                    new Label().Text("Status").FontSize(15).Row(Row.Status).Column(Col.Label),

                    new Label()
                        .Text(Binding.Create(static (ViewModel vm) => vm.Status))
                        .FontSize(15).Row(Row.Status).Column(Col.Input).Margin(5, 0)
                        .AutomationId("StatusLabel"),
                },
            }
            .ColumnDefinitions(Columns.Define((Col.Label, Auto), (Col.Input, Star)))
            .RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Code, Auto), (Row.Register, Auto), (Row.Status, Auto)))
            .ColumnSpacing(12).RowSpacing(12));
    }
}

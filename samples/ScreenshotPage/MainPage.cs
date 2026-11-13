using PureMarkup.Maui;
using static PureMarkup.Maui.GridRowsColumns;

namespace ScreenshotPage;

public class MainPage : ContentPage
{
    enum Row { Title, Code, Register, Reset, Status, Theme }

    enum Col { Label, Input }

    readonly ViewModel _vm = new();

    public MainPage()
    {
        this.BindingContext(_vm).Padding(24).BackgroundColor(Tokens.Page)
            .Content(new Grid
            {
                Children =
                {
                    new Label().Text("Registration").Font(size: 22, bold: true).TextColor(Tokens.Text)
                        .Row(Row.Title).Column(Col.Label, Col.Input),

                    new Label().Text("Code").FontSize(Tokens.Body).TextColor(Tokens.Text)
                        .VerticalTextAlignment(TextAlignment.Center)
                        .Row(Row.Code).Column(Col.Label),

                    // Look comes from the implicit Entry preset registered in App.
                    new Entry()
                        .Keyboard(Keyboard.Numeric).Placeholder("Enter number")
                        .Row(Row.Code).Column(Col.Input)
                        .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay))
                        .AutomationId("CodeEntry"),

                    new Button().Text("Register").Row(Row.Register).Column(Col.Input)
                        .OnClicked(_vm.Register)
                        .AutomationId("RegisterButton"),

                    AppPresets.DangerButton.New().Text("Reset").Row(Row.Reset).Column(Col.Input)
                        .OnClicked(_vm.Reset)
                        .AutomationId("ResetButton"),

                    new Label().Text("Status").FontSize(Tokens.Body).TextColor(Tokens.Text)
                        .Row(Row.Status).Column(Col.Label),

                    new Label()
                        .Text(Binding.Create(static (ViewModel vm) => vm.Status))
                        .FontSize(Tokens.Body).TextColor(Tokens.Text)
                        .Row(Row.Status).Column(Col.Input).Margin(5, 0)
                        .AutomationId("StatusLabel"),

                    new Button().Text("Toggle theme").Row(Row.Theme).Column(Col.Input)
                        .OnClicked(ToggleTheme)
                        .AutomationId("ThemeButton"),
                },
            }
            .ColumnDefinitions(Columns.Define((Col.Label, Auto), (Col.Input, Star)))
            .RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Code, Auto), (Row.Register, Auto),
                (Row.Reset, Auto), (Row.Status, Auto), (Row.Theme, Auto)))
            .ColumnSpacing(12).RowSpacing(12));
    }

    static void ToggleTheme()
    {
        var app = Application.Current!;
        app.UserAppTheme = app.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
    }
}

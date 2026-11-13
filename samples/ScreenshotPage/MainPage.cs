using PureMarkup.Maui;

namespace ScreenshotPage;

public class MainPage : ContentPage
{
    readonly ViewModel _vm = new();

    public MainPage()
    {
        this.BindingContext(_vm).Padding(new Thickness(24))
            .Content(new Grid
            {
                Children =
                {
                    new Label().Text("Registration").FontSize(22).FontAttributes(FontAttributes.Bold)
                        .Row(0).Column(0).ColumnSpan(2),

                    new Label().Text("Code").FontSize(15).VerticalTextAlignment(TextAlignment.Center)
                        .Row(1).Column(0),

                    new Entry()
                        .Keyboard(Keyboard.Numeric).BackgroundColor(Colors.AliceBlue)
                        .Row(1).Column(1)
                        .FontSize(15).Placeholder("Enter number").TextColor(Colors.Black)
                        .HeightRequest(44).Margin(new Thickness(5, 5))
                        .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay))
                        .AutomationId("CodeEntry"),

                    new Button().Text("Register").Row(2).Column(1).Margin(new Thickness(5, 5))
                        .OnClicked(_vm.Register)
                        .AutomationId("RegisterButton"),

                    new Label().Text("Status").FontSize(15).Row(3).Column(0),

                    new Label()
                        .Text(Binding.Create(static (ViewModel vm) => vm.Status))
                        .FontSize(15).Row(3).Column(1).Margin(new Thickness(5, 0))
                        .AutomationId("StatusLabel"),
                },
            }
            .ColumnDefinitions([new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)])
            .RowDefinitions([new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)])
            .ColumnSpacing(12).RowSpacing(12));
    }
}

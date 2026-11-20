namespace PureMarkupApp._1.Pages;

public class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		this.BindingContext(vm)
			.Content(new ScrollView().Content(new VerticalStackLayout
			{
				Children =
				{
					new Image().Source("dotnet_bot.png").HeightRequest(185).Aspect(Aspect.AspectFit)
						.SemanticDescription("dot net bot in a submarine number ten"),

					// Keyed styles from the template are presets: .New() makes a Label that uses one.
					DefaultLook.Headline.New().Text("Hello, World!")
						.HeadingLevel(SemanticHeadingLevel.Level1),

					DefaultLook.SubHeadline.New().Text("Welcome to \n.NET Multi-platform App UI")
						.HeadingLevel(SemanticHeadingLevel.Level2)
						.SemanticDescription("Welcome to dot net Multi platform App U I"),

					AppPresets.PrimaryButton.New()
						.Text(Binding.Create(static (MainViewModel m) => m.CounterText))
						.Command(vm.IncrementCommand)
						.Hint("Counts the number of times you click")
						.FillHorizontal()
						.AutomationId("CounterButton"),
				},
			}.Padding(30, 0).Spacing(25)));
	}
}

using PureMarkup.Maui;
using PureMarkup.Maui.Defaults;

namespace PureMarkupApp1;

public class NewPage1 : ContentPage
{
	public NewPage1()
	{
		this.Title("NewPage1").Padding(24)
			.Content(new VerticalStackLayout
			{
				Children =
				{
					DefaultLook.Headline.New().Text("NewPage1"),
					new Label().Text("Welcome to .NET MAUI in C# markup!").Center(),
				},
			}.Spacing(12));
	}
}

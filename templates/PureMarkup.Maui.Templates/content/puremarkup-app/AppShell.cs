namespace PureMarkupApp._1;

public class AppShell : Shell
{
	public AppShell()
	{
		this.Title("PureMarkupApp.1").FlyoutBehavior(FlyoutBehavior.Disabled);

#if (IncludeSampleContent)
		Items.Add(new TabBar
		{
			Items =
			{
				new ShellContent().Title("Home").Route("MainPage")
					.ContentTemplate(new DataTemplate(typeof(MainPage))),
				new ShellContent().Title("Todo").Route("TodoPage")
					.ContentTemplate(new DataTemplate(typeof(TodoPage))),
			},
		});
#else
		Items.Add(new ShellContent().Title("Home").Route("MainPage")
			.ContentTemplate(new DataTemplate(typeof(MainPage))));
#endif
	}
}

using Microsoft.Extensions.Logging;

namespace PureMarkupApp._1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Shell resolves pages registered here, so page constructors can take their view model.
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainViewModel>();
#if (IncludeSampleContent)
		builder.Services.AddTransient<TodoPage>();
		builder.Services.AddSingleton<TodoViewModel>();
#endif

//-:cnd:noEmit
#if DEBUG
		builder.Logging.AddDebug();
#endif
//+:cnd:noEmit

		return builder.Build();
	}
}

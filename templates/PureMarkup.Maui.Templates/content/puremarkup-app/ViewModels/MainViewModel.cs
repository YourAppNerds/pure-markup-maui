namespace PureMarkupApp._1.ViewModels;

public partial class MainViewModel : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CounterText))]
	public partial int Count { get; set; }

	public string CounterText => Count switch
	{
		0 => "Click me",
		1 => "Clicked 1 time",
		_ => $"Clicked {Count} times",
	};

	[RelayCommand]
	void Increment()
	{
		Count++;
		SemanticScreenReader.Announce(CounterText);
	}
}

using System.Collections.ObjectModel;

namespace PureMarkupApp._1.ViewModels;

public partial class TodoViewModel : ObservableObject
{
	public ObservableCollection<TodoItem> Items { get; } =
	[
		new("Write a page in C# markup"),
		new("Derive a preset with .With(...)"),
	];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(AddCommand))]
	public partial string? NewItem { get; set; }

	bool CanAdd() => !string.IsNullOrWhiteSpace(NewItem);

	[RelayCommand(CanExecute = nameof(CanAdd))]
	void Add()
	{
		Items.Add(new(NewItem!.Trim()));
		NewItem = null;
	}

	[RelayCommand]
	void ClearDone()
	{
		foreach (var item in Items.Where(i => i.IsDone).ToList())
			Items.Remove(item);
	}
}

public partial class TodoItem(string text) : ObservableObject
{
	public string Text { get; } = text;

	[ObservableProperty]
	public partial bool IsDone { get; set; }
}

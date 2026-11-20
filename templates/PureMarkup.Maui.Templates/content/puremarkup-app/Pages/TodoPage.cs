using static PureMarkup.Maui.GridRowsColumns;

namespace PureMarkupApp._1.Pages;

/// <summary>Sample page: enum-named grid rows, two-way bindings, a C# item template, presets.</summary>
public class TodoPage : ContentPage
{
	enum Row { Title, Input, List, Actions }

	enum Col { Text, Add }

	public TodoPage(TodoViewModel vm)
	{
		this.BindingContext(vm).Title("Todo").Padding(24)
			.Content(new Grid
			{
				Children =
				{
					DefaultLook.Headline.New().Text("Todo")
						.Row(Row.Title).Column(Col.Text, Col.Add),

					new Entry().Placeholder("What needs doing?")
						.Text(Binding.Create(static (TodoViewModel m) => m.NewItem, BindingMode.TwoWay))
						.ReturnCommand(vm.AddCommand)
						.Row(Row.Input).Column(Col.Text)
						.AutomationId("NewItemEntry"),

					AppPresets.PrimaryButton.New().Text("Add")
						.Command(vm.AddCommand)
						.Row(Row.Input).Column(Col.Add)
						.AutomationId("AddButton"),

					new CollectionView()
						.ItemsSource(vm.Items)
						.ItemTemplate(new DataTemplate(ItemView))
						.Row(Row.List).Column(Col.Text, Col.Add),

					AppPresets.DangerButton.New().Text("Clear done")
						.Command(vm.ClearDoneCommand)
						.Row(Row.Actions).Column(Col.Text, Col.Add)
						.AutomationId("ClearDoneButton"),
				},
			}
			.RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Input, Auto), (Row.List, Star), (Row.Actions, Auto)))
			.ColumnDefinitions(Columns.Define((Col.Text, Star), (Col.Add, Auto)))
			.RowSpacing(12).ColumnSpacing(12));
	}

	static Grid ItemView() => new Grid
	{
		Children =
		{
			new CheckBox()
				.IsChecked(Binding.Create(static (TodoItem i) => i.IsDone, BindingMode.TwoWay))
				.Column(0),
			new Label()
				.Text(Binding.Create(static (TodoItem i) => i.Text))
				.VerticalTextAlignment(TextAlignment.Center)
				.Column(1),
		},
	}
	.ColumnDefinitions(Columns.Define(Auto, Star))
	.ColumnSpacing(8);
}

# Migrating from CommunityToolkit.Maui.Markup

PureMarkup.Maui covers much of the same ground as CommunityToolkit.Maui.Markup.
The main difference: PureMarkup has a method for every bindable property and event.
So you rarely fall back to object initializers or `.Invoke(...)`.

Parts of PureMarkup's grid and layout helpers are adapted from the toolkit (MIT).
Many calls keep their names.

## Steps

1. Replace the `CommunityToolkit.Maui.Markup` package with `PureMarkup.Maui`.
2. Replace `using CommunityToolkit.Maui.Markup;` with `using PureMarkup.Maui;`.
3. Replace `using static CommunityToolkit.Maui.Markup.GridRowsColumns;` with `using static PureMarkup.Maui.GridRowsColumns;`.
4. Fix the calls in the table below.
5. Fix any `PM0007` warnings: they mark string-path bindings.

## Mapping

| CommunityToolkit.Maui.Markup | PureMarkup.Maui |
|---|---|
| `new Label { Text = "Hi" }` | `new Label().Text("Hi")` (both still compile) |
| `.Bind(Label.TextProperty, nameof(VM.Name))` | `.Text(Binding.Create(static (VM vm) => vm.Name))` |
| `.Bind(nameof(VM.Name))` (default property) | `.Text(Binding.Create(static (VM vm) => vm.Name))` |
| `.Bind(Entry.TextProperty, static (VM vm) => vm.Name, static (VM vm, string v) => vm.Name = v, BindingMode.TwoWay)` | `.Text(Binding.Create(static (VM vm) => vm.Name, BindingMode.TwoWay))` |
| `.Bind(..., converter: c)` | `.Text(Binding.Create(static (VM vm) => vm.Age, converter: c))` |
| `.BindCommand(nameof(VM.SaveCommand))` | `.Command(vm.SaveCommand)` |
| `.Invoke(b => b.Clicked += OnClick)` | `.OnClicked(OnClick)` |
| `.Assign(out var label)` | No equivalent. Declare a local: `var label = new Label()...;` |
| `.Row(1).Column(2)`, `.RowSpan(2)`, `.ColumnSpan(2)` | Same |
| `.Row(Row.Body).Column(Col.Input)` | Same |
| `.Column(Col.Label).ColumnSpan(2)` | `.Column(Col.Label, Col.Input)` (first to last), or the same |
| `Rows.Define(...)`, `Columns.Define(...)`, `Auto`, `Star`, `Stars(2)` | Same |
| `.Margins(left, top, right, bottom)` | `.Margin(left, top, right, bottom)` |
| `.Paddings(left, top, right, bottom)` | `.Padding(left, top, right, bottom)` |
| `.Center()`, `.CenterHorizontal()`, `.CenterVertical()` | Same |
| `.Start()`, `.End()`, `.Top()`, `.Bottom()` | Same |
| `.Fill()` | `.FillBoth()` |
| `.FillHorizontal()`, `.FillVertical()` | Same |
| `.TextCenter()` | `.HorizontalTextAlignment(TextAlignment.Center).VerticalTextAlignment(TextAlignment.Center)` |
| `.TextCenterHorizontal()`, `.TextStart()`, `.TextEnd()` | `.HorizontalTextAlignment(TextAlignment.Center / Start / End)` |
| `.TextCenterVertical()`, `.TextTop()`, `.TextBottom()` | `.VerticalTextAlignment(TextAlignment.Center / Start / End)` |
| `.Font(size, bold, italic, family)` | Same |
| `.Size(w, h)`, `.Width(w)`, `.Height(h)` | Same |
| `.TapGesture(handler)` | Same |
| `.BindTapGesture(nameof(VM.OpenCommand))` | `.TapGesture(() => vm.OpenCommand.Execute(null))` |
| `.SemanticDescription(...)` | Same |
| `.SemanticHint(...)` | `.Hint(...)` |
| `.SemanticHeadingLevel(...)` | `.HeadingLevel(...)` |
| `.DynamicResource(Label.TextColorProperty, "Key")` | A token: `.TextColor(Tokens.Text)`. In a preset: `p.DynamicResource(Label.TextColorProperty, "Key")` |
| `new Style<Entry>(...)` and `.Style(style)` | A `Preset<Entry>` and `.Use(preset)` |
| `AppThemeBinding` / `.AppThemeColorBinding(...)` | A `Token<Color>` (see [Presets and theming](presets-theming.md)) |
| `FuncConverter<TSource, TDest>` | `FuncConverter<TSource, TTarget>(convert, convertBack)` |

## Things that work differently

**Bindings.**
The toolkit's `.Bind` takes a bindable property and a path or getter.
PureMarkup has no `.Bind`.
Every bindable property method has a `BindingBase` overload instead.
Pass `Binding.Create(...)` to it.
MAUI's binding source generator compiles it, so it works with trimming and NativeAOT.

**Members made by other source generators.**
MAUI's binding generator cannot see members another source generator writes.
A `[RelayCommand]` command from CommunityToolkit.Mvvm is one example.
`Binding.Create(static (VM vm) => vm.SaveCommand)` then fails with `BSG0006`.
Pass the command itself: `.Command(vm.SaveCommand)`.
`[ObservableProperty]` on a `partial` property is fine, because you declare the property yourself.

**Styles.**
The toolkit's `Style<T>` maps to `Preset<T>`.
A preset uses the same fluent methods as controls.
See [Presets and theming](presets-theming.md).

**Performance.**
[Page construction benchmarks](page-construction-benchmarks.md) compare both libraries on the same page.

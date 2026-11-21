# Markup API

PureMarkup.Maui has one fluent method per member of each MAUI Controls type.
A source generator writes these methods at build time.
It covers every public type in `Microsoft.Maui.Controls`.

All methods are extension methods in the `PureMarkup.Maui` namespace.
Each one returns the element it was called on, with its exact type.
So a chain on an `Entry` stays an `Entry` to the end.

## How methods are named

### Properties

A property keeps its exact name.

```csharp
var entry = new Entry()
    .Placeholder("Email")            // Entry.PlaceholderProperty
    .Keyboard(Keyboard.Email)        // InputView.KeyboardProperty
    .IsSpellCheckEnabled(false);
```

A bindable property gets three overloads:

| Overload | Example |
|---|---|
| A value | `.TextColor(Colors.Red)` |
| A `BindingBase` | `.TextColor(Binding.Create(static (VM vm) => vm.Color))` |
| A `Token<T>` | `.TextColor(Tokens.Text)` (see [Presets and theming](presets-theming.md)) |

A plain settable property (one without a `BindableProperty`) gets only the value overload.

### Events

An event gets the prefix `On`.
`Button.Clicked` becomes `.OnClicked(...)`.

An event with the usual `(sender, args)` shape gets two overloads.
One takes the event's own delegate.
The other takes a plain `Action` for when you do not need the arguments.

```csharp
var button = new Button()
    .Text("Count")
    .OnClicked(() => count++)                    // Action
    .OnPressed((sender, e) => Log(sender));      // EventHandler
```

### Attached properties

An attached property is named after its `Set` method without `Set`.
It works on any `BindableObject`.

```csharp
new Label().Text("Total").Row(1).Column(0).ColumnSpan(2);   // Grid.SetRow, SetColumn, SetColumnSpan
new Label().Grow(1);                                         // FlexLayout.SetGrow
```

### Name collisions

Sometimes an attached property has the same name as another method on a related type.
Then it gets the name of its declaring type as a prefix.
A trailing `Properties` in that type name is dropped.

| MAUI member | Method |
|---|---|
| `Shell.BackgroundColor` | `.ShellBackgroundColor(...)` |
| `Shell.TitleColor` | `.ShellTitleColor(...)` |
| `Shell.TitleView` | `.ShellTitleView(...)` |
| `NavigationPage.TitleView` | `.NavigationPageTitleView(...)` |
| `SemanticProperties.Description` | `.SemanticDescription(...)` |
| `ToolTipProperties.Text` | `.ToolTipText(...)` |

Attached properties without a clash keep the short name.
`SemanticProperties.Hint` is `.Hint(...)`, and `SemanticProperties.HeadingLevel` is `.HeadingLevel(...)`.

```csharp
new ContentPage().ShellBackgroundColor(Colors.Black).ShellTitleColor(Colors.White);
new Image().SemanticDescription("Company logo").Hint("Opens the home page").ToolTipText("Home");
```

If a name still clashes after the prefix, the method is skipped with info diagnostic `PM0003`.
A public instance method of the same name that takes an argument also skips it, because C# would call that method instead.
A member named like a C# keyword gets an `@` prefix.

### What is not generated

| Member | Why | Diagnostic |
|---|---|---|
| Read-only bindable property | Nothing to set. | `PM0001` |
| Delegate-typed property | Not a markup value. | `PM0002` |
| Members of generic types | An extension method cannot infer the type arguments. | `PM0004` |
| Attached property on a non-`BindableObject` target | No receiver to extend. | `PM0005` |
| `[Obsolete]` or `[EditorBrowsable(Never)]` members | Hidden on purpose. | none |
| Types in `Internals`, `PlatformConfiguration`, `Compatibility` and `Design` namespaces | Not app-facing. | none |

Collections such as `Layout.Children` are get-only.
You fill them with a collection initializer (see below).

See [Diagnostics](diagnostics.md) for details on each ID.

## Bindings

Bind with MAUI's `Binding.Create` and a static lambda:

```csharp
var name = new Entry()
    .Text(Binding.Create(static (ProfileViewModel m) => m.Name, BindingMode.TwoWay));

var busy = new ActivityIndicator()
    .IsRunning(Binding.Create(static (ProfileViewModel m) => m.IsBusy));
```

MAUI's binding source generator intercepts each `Binding.Create(lambda)` call.
It turns the lambda into a compiled, typed binding.
That binding needs no reflection.
So it works with trimming and NativeAOT.

`Binding.Create` also takes a converter, a string format and a source:

```csharp
var age = new Label()
    .Text(Binding.Create(static (ProfileViewModel m) => m.Age,
        converter: new FuncConverter<int, string>(static a => a < 18 ? "Minor" : "Adult")));
```

`FuncConverter<TSource, TTarget>` takes a convert function and an optional convert-back function.

Markup methods take any `BindingBase`.
A string-path binding such as `new Binding("Name")` compiles.
But it needs reflection and breaks under trimming.
Analyzer `PM0007` warns when one reaches a markup method.

You can skip a binding when the value does not change.
Pass a command straight from the view model: `.Command(vm.SaveCommand)`.

Set the binding context like any other property: `this.BindingContext(vm)`.

## Layouts and children

`Children` is get-only, so fill it with a collection initializer.
Then chain methods after the closing brace:

```csharp
var stack = new VerticalStackLayout
{
    Children =
    {
        new Label().Text("One"),
        new Label().Text("Two"),
    },
}.Spacing(8).Padding(16);

var page = new ContentPage().Content(new ScrollView().Content(stack));
```

Single-child containers such as `ContentPage`, `ScrollView`, `Border` and `ContentView` use `.Content(...)`.

### Grids

`.Row`, `.Column`, `.RowSpan` and `.ColumnSpan` come from `Grid`'s attached properties.
Add `using static PureMarkup.Maui.GridRowsColumns;` for row and column helpers.
With enums, rows and columns get names:

```csharp
enum Row { Title, Body, Actions }
enum Col { Label, Input }

var grid = new Grid
{
    Children =
    {
        new Label().Text("Title").Row(Row.Title).Column(Col.Label, Col.Input),  // spans both columns
        new Label().Text("Name").Row(Row.Body).Column(Col.Label),
        new Entry().Row(Row.Body).Column(Col.Input),
    },
}
.RowDefinitions(Rows.Define((Row.Title, Auto), (Row.Body, Star), (Row.Actions, Auto)))
.ColumnDefinitions(Columns.Define((Col.Label, Auto), (Col.Input, Star)))
.RowSpacing(8).ColumnSpacing(12);

grid.Add(new Button().Text("OK").Row(Row.Actions).Column(Col.Input));
```

`.Column(first, last)` and `.Row(first, last)` set the span from the first to the last name.
Without names, pass sizes in order:

```csharp
new Grid().RowDefinitions(Rows.Define(Auto, Star)).ColumnDefinitions(Columns.Define(Stars(2), Star));
```

### Shorthands

A small hand-written layer adds shorthands on top of the generated methods.
Examples: `.Height(44)`, `.Size(w, h)`, `.Margin(h, v)`, `.Padding(l, t, r, b)`, `.Center()`,
`.FillHorizontal()`, `.Font(size, bold, italic, family)` and `.TapGesture(...)`.
The README lists them all.

## Generating for your own controls

The package ships the generator.
So your app can generate methods for its own controls, or for another library's.
Add an assembly attribute anywhere in the project:

```csharp
[assembly: PureMarkup.Maui.GenerateMarkup(typeof(MyApp.Controls.RatingView))]
```

Then `RatingView` gets the same kind of methods as MAUI's controls:

```csharp
new RatingView().Value(4).OnValueChanged((s, v) => { }).Margin(8);
```

The attribute has two optional settings:

| Setting | Effect |
|---|---|
| `WholeAssembly = true` | Generate for every public type in the assembly that declares the given type. |
| `Namespace = "..."` | The namespace of the generated classes. The default is `PureMarkup.Maui`. |

```csharp
[assembly: PureMarkup.Maui.GenerateMarkup(typeof(Some.Library.Chart), WholeAssembly = true, Namespace = "MyApp.Markup")]
```

Things to know:

- The generator adds the attribute class to your project for you.
  It is `[Conditional]`, so it does not end up in your compiled assembly.
- The project must reference `Microsoft.Maui.Controls`.
  Otherwise you get warning `PM0006`.
- Types that derive from `VisualElement` also get preset overloads, so you can style them with `Preset<T>`.
- A `WholeAssembly` walk over a referenced assembly is cached.
  Editing your own code does not re-run it.
- MAUI's own methods are not generated again in your project.
  They come compiled in the PureMarkup.Maui library.

# PureMarkup.Maui

C# markup for .NET MAUI without XAML. A Roslyn source generator emits fluent extension
methods for MAUI Controls types, so a page reads like this:

```csharp
new Entry()
    .Keyboard(Keyboard.Numeric).Row(1).Column(1)
    .Placeholder("Enter number").HeightRequest(44)
    .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay))
```

Per bindable property there is a value and a `BindingBase` overload, generic over the receiver
so chains keep their type. Attached properties (`Grid.Row` -> `.Row(int)`), events
(`.OnClicked(...)`) and plain settable properties are covered too. No reflection, no expression
trees: the library is trimmable and AOT-compatible. Skipped members get `PM000x` info diagnostics.

## Layout
- `src/PureMarkup.Maui.Generators/`: the incremental generator (netstandard2.0).
- `src/PureMarkup.Maui/`: MAUI class library; `GenerateMarkup.cs` runs the generator over all of
  Microsoft.Maui.Controls at build time. TFMs: net10.0, net10.0-windows10.0.19041.0
  (mobile TFMs with `-p:PureMarkupMobile=true`).
- `tests/PureMarkup.Maui.Generators.Tests/`: xUnit + Verify snapshot tests, a run against the real
  MAUI reference, and a compile test of the snippet above.
- `spikes/`: throwaway experiments, outside the shared build settings.

Your own controls: `[assembly: PureMarkup.Maui.GenerateMarkup(typeof(MyControl))]`
(`Namespace = "..."` picks the output namespace).

## Build and test
```
dotnet build PureMarkup.Maui.slnx
dotnet test PureMarkup.Maui.slnx
```

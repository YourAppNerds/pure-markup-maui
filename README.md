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

## Sugar
A small hand-written layer in `src/PureMarkup.Maui/Sugar/` (same namespace) adds shorthands:
`.Height(44)`, `.Width()`, `.Size(w, h)`, `.Margin(h, v)`, `.Padding(l, t, r, b)`, `.Center()`,
`.Start()`, `.End()`, `.Top()`, `.Bottom()`, `.FillBoth()`, `.Font(size, bold, italic, family)`,
`.TapGesture(...)`, `.SwipeGesture(...)`, `.PanGesture(...)`, `.PinchGesture(...)`,
`FuncConverter<TSource, TTarget>`, and enum-named grids
(`using static PureMarkup.Maui.GridRowsColumns;`): `Rows.Define((Row.Title, Auto), (Row.Body, Star))`,
`.Row(Row.Body)`, `.Column(Col.Label, Col.Input)` (with span). Parts adapted from
CommunityToolkit.Maui.Markup (MIT, see `THIRD-PARTY-NOTICES.md`).

## Layout
- `src/PureMarkup.Maui.Generators/`: the incremental generator (netstandard2.0). The walk over
  a whole referenced assembly is keyed on assembly identity + attribute arguments, so source
  edits do not re-run it.
- `src/PureMarkup.Maui/`: MAUI class library; `GenerateMarkup.cs` runs the generator over all of
  Microsoft.Maui.Controls at build time. TFMs: net10.0, net10.0-windows10.0.19041.0
  (mobile TFMs with `-p:PureMarkupMobile=true`).
- `tests/PureMarkup.Maui.Generators.Tests/`: xUnit + Verify: generator snapshots, real-MAUI run,
  snippet compile, incremental caching, and `PublicApi.verified.txt` (public API baseline; an
  API change fails the test until the snapshot is accepted).
- `samples/ScreenshotPage/`: Windows MAUI app with no XAML files; `verify-ui.ps1` drives it.
- `spikes/`: throwaway experiments, outside the shared build settings.

Your own controls: `[assembly: PureMarkup.Maui.GenerateMarkup(typeof(MyControl))]`
(`Namespace = "..."` picks the output namespace). The package ships the generator, so this
works in consuming apps; the MAUI methods themselves are not regenerated there.

## Build, test, pack
```
dotnet build PureMarkup.Maui.slnx
dotnet test PureMarkup.Maui.slnx
dotnet pack src/PureMarkup.Maui -c Release -o artifacts/packages
```
The nupkg (0.1.0-dev) holds `lib/` for both TFMs and the generator in `analyzers/dotnet/cs/`.

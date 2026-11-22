# PureMarkup.Maui

C# markup for .NET MAUI without XAML. A Roslyn source generator emits fluent extension
methods for MAUI Controls types, so a page reads like this:

```csharp
new Entry()
    .Keyboard(Keyboard.Numeric).Row(1).Column(1)
    .Placeholder("Enter number").HeightRequest(44)
    .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay))
```

Per bindable property there is a value, a `BindingBase` and a `Token<T>` overload, generic over the receiver
so chains keep their type. Attached properties (`Grid.Row` -> `.Row(int)`), events
(`.OnClicked(...)`) and plain settable properties are covered too. No reflection, no expression
trees: the library is trimmable and AOT-compatible, with 0 trim/AOT analyzer warnings, and a
trimmed (and, as a test, NativeAOT) Windows publish of the sample runs; the iOS/Mac Catalyst
NativeAOT check still needs a Mac (`docs/trimming-aot.md`). Skipped members get `PM000x` info
diagnostics. Bind with `Binding.Create(static (VM vm) => vm.Prop)`: analyzer `PM0007` warns when a
string-path `Binding` reaches a markup method (it needs reflection and breaks under trimming).

## Docs
[Getting started](docs/getting-started.md) · [Markup API](docs/markup-api.md) ·
[Presets and theming](docs/presets-theming.md) · [Diagnostics](docs/diagnostics.md) ·
[Migrating from CommunityToolkit.Maui.Markup](docs/migrating-from-communitytoolkit-markup.md) ·
[all docs](docs/README.md)

## Sugar
A small hand-written layer in `src/PureMarkup.Maui/Sugar/` (same namespace) adds shorthands:
`.Height(44)`, `.Width()`, `.Size(w, h)`, `.Margin(h, v)`, `.Padding(l, t, r, b)`, `.Center()`,
`.Start()`, `.End()`, `.Top()`, `.Bottom()`, `.FillBoth()`, `.Font(size, bold, italic, family)`,
`.TapGesture(...)`, `.SwipeGesture(...)`, `.PanGesture(...)`, `.PinchGesture(...)`,
`FuncConverter<TSource, TTarget>`, and enum-named grids
(`using static PureMarkup.Maui.GridRowsColumns;`): `Rows.Define((Row.Title, Auto), (Row.Body, Star))`,
`.Row(Row.Body)`, `.Column(Col.Label, Col.Input)` (with span). Parts adapted from
CommunityToolkit.Maui.Markup (MIT, see `THIRD-PARTY-NOTICES.md`).

## Presets and tokens
A `Preset<T>` is written with the same methods and compiles to one cached MAUI `Style`:
```csharp
public static Preset<Entry> Field => Preset.Once<Entry>(static p => p
    .FontSize(Tokens.Body).Height(44).TextColor(Tokens.Text)
    .State(VisualStateManager.CommonStates.Disabled, static s => s.Opacity(0.5)));
public static Preset<Entry> Numeric => Field.With(static p => p.Keyboard(Keyboard.Numeric)); // BasedOn

new Entry().Use(AppPresets.Numeric).FontSize(18);   // local value wins, in any order
AppPresets.Field.New();                              // new Entry().Use(...)
this.UsePresets(r => r.Default(AppPresets.Field));   // in App(): implicit look for plain new Entry()
```
`Token<T>` (`new Token<Color>(light, dark)`) works on elements and in presets and follows the app
theme live. `this.UsePresets(r => r.UseDefaultLook())` applies the stock MAUI template's look
(`Styles.xaml` + `Colors.xaml`) to 23 control and page types; derive with `DefaultLook.Button.With(...)`.
Precedence rules, triggers, tokens, the net10 theme workaround and Hot Reload:
[docs/presets-theming.md](docs/presets-theming.md).

## Templates
`PureMarkup.Maui.Templates` (`templates/PureMarkup.Maui.Templates/`) holds two `dotnet new`
templates with no XAML files on any platform: `puremarkup-app` and `puremarkup-page`. Install with
`dotnet new install PureMarkup.Maui.Templates@0.1.0-preview.2`; [docs/getting-started.md](docs/getting-started.md)
lists the options, covers the NETSDK1147 restore error on machines without every MAUI workload,
and shows how to use a local build. Android (emulator, Debug and trimmed Release):
[docs/android.md](docs/android.md). Template sources are under `content/`; `Microsoft.TemplateEngine.Authoring.Tasks`
validates them on pack. The template references PureMarkup.Maui at the library's `<Version>` (a
check in the verify script).

`scripts/verify-template.ps1` (Windows PowerShell) packs both packages, installs the templates into
a private hive, instantiates default, `--sample-content`, `--no-windows` and `--framework net11.0`
into a temp folder with `artifacts/packages` as a feed, checks for zero `.xaml`, builds the Windows
TFM, runs the default and sample apps through UI Automation, builds `net10.0-android` of the
no-windows and sample apps, adds a page with `puremarkup-page` and rebuilds. The Android builds
report SKIP when restore fails with NETSDK1147 (workload missing).

## Layout
- `src/PureMarkup.Maui.Generators/`: the incremental generator (netstandard2.0). The walk over
  a whole referenced assembly is keyed on assembly identity + attribute arguments, so source
  edits do not re-run it.
- `src/PureMarkup.Maui/`: MAUI class library; `GenerateMarkup.cs` runs the generator over all of
  Microsoft.Maui.Controls at build time. TFMs: net10.0, net10.0-windows10.0.19041.0
  (mobile TFMs with `-p:PureMarkupMobile=true`; the package ships `lib/net10.0`, which Android
  apps use).
- `tests/PureMarkup.Maui.Generators.Tests/`: xUnit + Verify: generator snapshots, real-MAUI run,
  snippet compile, incremental caching, and `PublicApi.verified.txt` (public API baseline; an
  API change fails the test until the snapshot is accepted).
- `samples/ScreenshotPage/`: Windows MAUI app with no XAML files (Android with
  `-p:PureMarkupMobile=true`), styled by `UseDefaultLook()` plus `AppPresets.cs`;
  `verify-ui.ps1` drives it (input, theme toggle, light and dark screenshots).
- `benchmarks/IntelliSense/`: console tool measuring completion latency and per-keystroke
  generator work in a consumer project; results in `docs/intellisense-speed.md`. Not packed.
- `benchmarks/PageConstruction/`: BenchmarkDotNet, the same page built with plain setters,
  PureMarkup, CommunityToolkit.Maui.Markup, shared Styles and presets; results in
  `docs/page-construction-benchmarks.md`. Not packed.
- `spikes/`: throwaway experiments, outside the shared build settings.

Your own controls: `[assembly: PureMarkup.Maui.GenerateMarkup(typeof(MyControl))]`
(`Namespace = "..."` picks the output namespace). The package ships the generator, so this
works in consuming apps; the MAUI methods themselves are not regenerated there. Naming rules and
options: [docs/markup-api.md](docs/markup-api.md).

## Build, test, pack
```
dotnet build PureMarkup.Maui.slnx
dotnet test PureMarkup.Maui.slnx
dotnet pack src/PureMarkup.Maui -c Release -o artifacts/packages
dotnet pack templates/PureMarkup.Maui.Templates -c Release -o artifacts/packages
powershell -File scripts/verify-template.ps1
dotnet run -c Release --project benchmarks/IntelliSense
dotnet run -c Release --project benchmarks/PageConstruction -- --filter *
```
The nupkg (0.1.0-preview.2) holds `lib/` for both TFMs and the generator in `analyzers/dotnet/cs/`.

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
trees: the library is trimmable and AOT-compatible, with 0 trim/AOT analyzer warnings, and a
trimmed (and, as a test, NativeAOT) Windows publish of the sample runs; the iOS/Mac Catalyst
NativeAOT check still needs a Mac (`docs/trimming-aot.md`). Skipped members get `PM000x` info
diagnostics. Bind with `Binding.Create(static (VM vm) => vm.Prop)`: analyzer `PM0007` warns when a
string-path `Binding` reaches a markup method (it needs reflection and breaks under trimming).

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
MAUI specificity applies: local values beat preset values; `.State(...)`, `.Trigger(...)` and
`.DataTrigger(...)` setters beat local values while active; `.With` beats its base; `.Use` replaces
an earlier `.Use` and beats the implicit default where both set a property (other implicit setters
still apply underneath). One exception: visual-state setters of an implicit default
(`r.Default(...)`, `UseDefaultLook()`) rank below local values, as with XAML implicit styles, so
`new Entry().BackgroundColor(Red)` under a default with a Disabled state stays red when disabled,
while the same state from `.Use(...)` still wins. `.Also(e => ...)` covers events and plain properties; it runs at
`.Use`/`.New`, not for implicit defaults. `Token<T>` (`new Token<Color>(light, dark)`) works on
elements and in presets. `AppThemeBinding` is internal on net10, so tokens are dynamic resources:
`UsePresets` (or `Theme.Attach(app)`) writes them into `app.Resources` and rewrites them on
`RequestedThemeChanged`, so theme switches update live. On Hot Reload a `MetadataUpdateHandler`
clears the preset cache and swaps the recompiled Styles into live elements (implicit defaults and
`.Use`); tracking is weak and only on when the runtime supports metadata updates.

## Default look
`PureMarkup.Maui.Defaults` ports the MAUI app template's `Styles.xaml` and `Colors.xaml`:
`this.UsePresets(r => r.UseDefaultLook())` registers `DefaultLook` presets as implicit styles for
23 control and page types (with their Disabled states and Switch On/Off), colors come from
`DefaultColors` and the light/dark `DefaultTokens`. Derive with `DefaultLook.Button.With(...)`; a
later `r.Default(...)` for the same type replaces that type's default. `Headline` and
`SubHeadline` are the template's keyed label styles. The font is the template's `OpenSansRegular`
alias (`UseDefaultLook(fontFamily: null)` keeps the platform font). As with XAML implicit styles,
visual-state setters of a default rank below local values.

## Templates
`PureMarkup.Maui.Templates` (`templates/PureMarkup.Maui.Templates/`) holds two `dotnet new`
templates with no XAML files on any platform:
```
dotnet new install PureMarkup.Maui.Templates
dotnet new puremarkup-app -n MyApp
dotnet new puremarkup-page -n SettingsPage -o Pages
```
`puremarkup-app` options: `--framework net10.0|net11.0` (net11.0 is wired but not built yet),
`--applicationId`, `--sample-content` (a Todo tab: enum-named grid, two-way bindings, a C# item
template), `--no-windows` (drops the Windows TFM and `Platforms/Windows`), `--puremarkup-version`.
The app has `MauiProgram.cs`, `App.cs` (`UseDefaultLook()`), `AppShell.cs`, `Pages/MainPage.cs`,
`ViewModels/MainViewModel.cs` (CommunityToolkit.Mvvm), `Resources/Theme/Tokens.cs` and
`AppPresets.cs` (presets derived with `DefaultLook.X.With(...)`), the stock template's images and
fonts, `IsAotCompatible`, and on Windows the C# WinUI host from `spikes/windows-no-appxaml`
(`Platforms/Windows/App.cs` + `Program.cs`, `AppxGeneratePriEnabled`). `puremarkup-page` takes the
project's `RootNamespace` unless `--namespace` is given. Template sources are under `content/`;
`Microsoft.TemplateEngine.Authoring.Tasks` validates them on pack. The template references
PureMarkup.Maui at the library's `<Version>` (a check in the verify script).

`scripts/verify-template.ps1` (Windows PowerShell) packs both packages, installs the templates into
a private hive, instantiates default, `--sample-content`, `--no-windows` and `--framework net11.0`
into a temp folder with `artifacts/packages` as a feed, checks for zero `.xaml`, builds the Windows
TFM, runs the default and sample apps through UI Automation, adds a page with `puremarkup-page` and
rebuilds. Mobile TFMs report SKIP when restore fails with NETSDK1147 (workload missing).

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
- `samples/ScreenshotPage/`: Windows MAUI app with no XAML files, styled by `UseDefaultLook()`
  plus `AppPresets.cs`;
  `verify-ui.ps1` drives it (input, theme toggle, light and dark screenshots).
- `benchmarks/IntelliSense/`: console tool measuring completion latency and per-keystroke
  generator work in a consumer project; results in `docs/intellisense-speed.md`. Not packed.
- `benchmarks/PageConstruction/`: BenchmarkDotNet, the same page built with plain setters,
  PureMarkup, CommunityToolkit.Maui.Markup, shared Styles and presets; results in
  `docs/page-construction-benchmarks.md`. Not packed.
- `spikes/`: throwaway experiments, outside the shared build settings.

Your own controls: `[assembly: PureMarkup.Maui.GenerateMarkup(typeof(MyControl))]`
(`Namespace = "..."` picks the output namespace). The package ships the generator, so this
works in consuming apps; the MAUI methods themselves are not regenerated there.

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
The nupkg (0.1.0-dev) holds `lib/` for both TFMs and the generator in `analyzers/dotnet/cs/`.

# Trimming and NativeAOT

Status on Windows, 2026-11-20 (MAUI Controls 10.0.20, .NET SDK 10.0.403). MAUI NativeAOT is
supported on iOS and Mac Catalyst only; the real check needs a Mac (see the end).

## Library
`src/PureMarkup.Maui` sets `IsAotCompatible` and `IsTrimmable`. That should run the trim, AOT and
single-file analyzers, but MAUI's `Microsoft.Maui.Controls.Common.targets` sets
`EnableTrimAnalyzer=false` and `SuppressTrimAnalysisWarnings=true` unless `TrimMode=full` or
`PublishAot=true`, so until now only the AOT and single-file analyzers ran. The csproj now sets
`_MauiDoNotConfigureTrimAnalyzer=true`; all three analyzers run on every build (checked with
`dotnet msbuild -getProperty` for both TFMs, and with a probe file that drew IL2026, IL2057 and
IL3000).

Result: **0 IL2xxx/IL3xxx warnings** in the library on net10.0 and net10.0-windows10.0.19041.0,
including the ~1583 generated methods. No suppressions. The library uses no reflection or
expression trees; `typeof(T)` goes only to `Style`/`Trigger` constructors.

## String-path bindings (PM0007)
No generated or sugar API takes a string binding path: every binding overload takes
`BindingBase`, and the binding call happens inside the library, so MAUI's compiled-binding
interceptor (which rewrites only direct `Binding.Create`, `BindingBase.Create` and `SetBinding`
calls with a lambda) never sees it. Callers must write
`.Text(Binding.Create(static (VM vm) => vm.Name))`.

MAUI marks `new Binding(string ...)` and `SetBinding(..., string path)` with
`[RequiresUnreferencedCode]`, but because of the targets above most MAUI apps never see that
IL2026. The generator package therefore ships analyzer **PM0007** (warning): a
`Microsoft.Maui.Controls.Binding` (or subclass) passed to any PureMarkup method, generated (also
in a consumer's own `[GenerateMarkup]` output) or hand-written (sugar, `PresetContext.DataTrigger`).
It looks at the argument's type, so `new Binding("x")`, `new Binding { Path = "x" }` and a
`Binding`-typed variable are all reported. Not covered: a string-path binding hidden behind a
`BindingBase`-typed variable or nested in a `MultiBinding`.

## Sample publish on Windows
`samples/ScreenshotPage` has opt-in switches that set the publish properties for that project only
(a global `-p:PublishTrimmed=true` reaches the netstandard2.0 generator and fails with NETSDK1124):

```
dotnet publish samples/ScreenshotPage -c Release -r win-x64 -p:PureMarkupTrimCheck=true -o artifacts/trim
dotnet publish samples/ScreenshotPage -c Release -r win-x64 -p:PureMarkupTrimCheck=true -p:PureMarkupAotCheck=true -o artifacts/aot
powershell -File samples/ScreenshotPage/verify-ui.ps1 -Exe artifacts/trim/ScreenshotPage.exe -Shot artifacts/trim.png
```
The AOT link step needs `vswhere.exe` on `PATH` (`C:\Program Files (x86)\Microsoft Visual Studio\Installer`).

| Publish | IL warnings | From PureMarkup | UI check (`verify-ui.ps1`) |
|---------|-------------|-----------------|----------------------------|
| `PublishTrimmed` (ILLink, partial trim) | 40 | 0 | pass |
| `PublishAot` (ILC) | 0 reported | 0 | pass |

The 40 ILLink warnings: 35 IL2081 in CsWinRT's `ABI.System.Collections.Generic.*` projections,
3 IL2026 in MAUI Controls (`AddControlsHandlers` -> `HybridWebViewHandler`,
`TypeConversionHelper.TryConvertUsingImplicitCastOperator`) and 2 IL2072 in MAUI's
`ShellContent.ApplyQueryAttributes` (the `[QueryProperty]` path). Trimming cut
`PureMarkup.Maui.dll` from 385 KB to 119 KB. Both published apps pass the UI script: typed
bindings (`Binding.Create`), presets with `.With`, implicit `UseDefaultLook()` and the live theme
switch all work, so nothing PureMarkup needs at runtime is trimmed away. MAUI does not support
NativeAOT on Windows; the AOT run is evidence only.

The sample's WinUI `App` is now `partial`, which clears CsWinRT1028 (reported once the AOT
settings are on).

## Android
A trimmed Release build of the template app has 0 trim warnings from PureMarkup, and the app runs
on the emulator ([android.md](android.md)).

## Still needs a Mac
- `dotnet publish -f net10.0-ios -p:PublishAot=true` (and maccatalyst) of the template app or a
  sample: the supported NativeAOT targets, with the iOS ILC warnings and a launch on a
  device or simulator.

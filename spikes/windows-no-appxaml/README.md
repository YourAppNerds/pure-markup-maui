# Spike: MAUI Windows host without `Platforms/Windows/App.xaml`

**Result: works, with one required MSBuild property.** A net10.0-windows10.0.19041.0 MAUI app
(MAUI 10.0.100 workload, Windows App SDK 1.7.250909003) runs with no XAML files at all,
unpackaged, in Debug and Release. Verified 2026-11-12 on Windows 11 by launching the exe and
driving it with UI Automation: window opens, Fluent (dark) styling renders, Entry takes
keyboard focus and typed text, Button click handler runs, `CloseMainWindow` exits with code 0.
A `Shell` with a locked flyout (MAUI's XAML-defined `RootNavigationView`) also renders
(`SPIKE_SHELL=1`). Screenshots: `screenshot-debug.png`, `screenshot-shell.png`.

## Files that replace App.xaml + App.xaml.cs
- `Platforms/Windows/App.cs`: `class App : MauiWinUIApplication, IXamlMetadataProvider`.
  Forwards metadata lookups to the three library providers the XAML compiler would have
  chained: `Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider`,
  `Microsoft.Maui.Controls.Controls_Core_XamlTypeInfo.XamlMetaDataProvider`,
  `Microsoft.Maui.Core_XamlTypeInfo.XamlMetaDataProvider` (constructible type wins, as in the
  generated `CheckOtherMetadataProvidersForType`).
- `Platforms/Windows/Program.cs`: `[STAThread] Main` copied from the generated `App.g.i.cs`:
  `ComWrappersSupport.InitializeComWrappers()`, `Application.Start(...)`, set
  `DispatcherQueueSynchronizationContext`, `new App()`. `DISABLE_XAML_GENERATED_MAIN` is not
  needed: with no `ApplicationDefinition` item the XAML compiler generates nothing.
- csproj: `<AppxGeneratePriEnabled>true</AppxGeneratePriEnabled>`.
- MAUI layer: `App.cs` (`Application`, `CreateWindow`), `MainPage.cs`; no `Resources/Styles`.

## What failed along the way (and why)
1. Without `AppxGeneratePriEnabled`: crash 0xC000027B, `Cannot locate resource from
   'ms-appx:///Microsoft.UI.Xaml/Themes/themeresources.xaml'` in `XamlControlsResources..ctor`.
   `Microsoft.Build.Msix.Pri.targets` only enables PRI generation when there are XAML
   `Page`/`ApplicationDefinition` items or `WindowsPackageType=MSIX`, so no `resources.pri`.
2. Without `IXamlMetadataProvider` on App: crash 0xC000027B, `Cannot find a resource with the
   given key: AcrylicBackgroundFillColorDefaultBrush` (plain page and Shell alike).
- Not needed: merging `XamlControlsResources` by hand. MAUI's `MauiCoreInitializer` already
  calls `Resources.AddLibraryResources<XamlControlsResources>()` during `CreateMauiApp`.

## Caveats
- The provider list is hard-coded. A NuGet/library that ships its own WinUI XAML (own
  `*_XamlTypeInfo.XamlMetaDataProvider`) must be added by hand, or the template needs a small
  helper/source generator that discovers them. The generated code is what normally does this.
- Provider class names are generated-code internals of WinUI/MAUI; could change across versions.
- Packaged (MSIX) not tested; PRI is on by default there, provider still required.
- Only `dotnet build` tested; trimmed/NativeAOT publish not tested.
- `App` ctor logs `UnhandledException` to `%TEMP%\WindowsNoAppXaml.log` (spike diagnostics only).
- Hot Reload not tested from the CLI. C# Hot Reload should be unaffected; there is no XAML to
  hot-reload. VS "XAML Live Preview"/Live Visual Tree for the WinUI layer was not checked.
- Theme follows the OS (dark here). `RequestedTheme` can no longer be set in App.xaml; set
  `Application.Current.UserAppTheme` in MAUI or `RequestedTheme` in the App ctor instead.

## Can the template drop the file?
Yes: ship `App.cs` + `Program.cs` + `AppxGeneratePriEnabled=true` (ideally set by the
PureMarkup.Maui build props, with a reusable metadata-provider base class in the library).
Main open item before relying on it: auto-discovering third-party metadata providers.

## Run it
`dotnet build -f net10.0-windows10.0.19041.0` then
`bin/Debug/net10.0-windows10.0.19041.0/win-x64/WindowsNoAppXaml.exe` (`SPIKE_SHELL=1` for Shell).

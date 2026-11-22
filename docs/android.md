# Android

First Android check of PureMarkup.Maui 0.1.0-preview.2, run on 2026-11-21.
Test setup: Windows 11, .NET SDK 10.0.403, workloads android 36.1.118, ios and maccatalyst
27.2.10000, maui-windows 10.0.110.
The device was the Android emulator: AVD `Pixel_10_Pro_XL`, API 37, x86_64, 16 KB pages.
Screenshots were taken but are not committed.

## Build

| What | `net10.0-android` | `net10.0-ios` | `net10.0-maccatalyst` |
|---|---|---|---|
| Library (`-p:PureMarkupMobile=true`) | builds, 0 warnings | builds, 0 warnings | builds, 0 warnings |
| `samples/ScreenshotPage` (`-p:PureMarkupMobile=true`) | builds, 0 warnings | not a target | not a target |
| `puremarkup-app` (default) | builds, 0 warnings | builds, 0 warnings | builds, 0 warnings |
| `puremarkup-app --sample-content` | builds, 0 warnings | builds, 0 warnings | builds, 0 warnings |

The template apps were built with a plain `dotnet build`, so every target framework they list was
built in one run, with no `-p:TargetFrameworks=` override.
The iOS and Mac Catalyst builds on Windows compile the app but make no app bundle; that step
needs a Mac.

Android apps use the package's `lib/net10.0` assembly.
The package ships no Android-specific build.
The library has no platform code, so this is enough.

## On the emulator

All three apps ran as Debug builds (`dotnet build -t:Install -f net10.0-android`).

| App | Checked | Result |
|---|---|---|
| `puremarkup-app` | Launch, `UseDefaultLook()` colors and OpenSans font, counter button binding, system dark mode (`cmd uimode night yes`) | Pass. The theme switches live, with no restart. |
| `puremarkup-app --sample-content` | Shell tab bar (Home, Todo), enum-named grid, two-way `Entry`, Add command, item template with `CheckBox`, Clear done, `Token<Color>` on the danger button in light and dark | Pass |
| `samples/ScreenshotPage` | Two-way `Entry` in both directions (type, Register, Reset), `DangerButton` preset, in-app theme toggle (`UserAppTheme`) | Pass |

Logcat showed no managed exceptions from these runs.

## Release and trimming

`dotnet build -c Release -f net10.0-android` of the `--sample-content` app trims and AOT-compiles
by default (`TrimMode=partial`, `RunAOTCompilation=true`).
The build reports 0 warnings, but MAUI hides trim warnings by default.
So it was built again with the warnings turned on:

```
-p:SuppressTrimAnalysisWarnings=false -p:_MauiDoNotConfigureTrimAnalyzer=true
-p:EnableTrimAnalyzer=true -p:TrimmerSingleWarn=false
```

| Trim mode | Warnings | From PureMarkup |
|---|---|---|
| `partial` (default) | 22 | 0 |
| `full` | 0 | 0 |

All 22 come from MAUI itself: `HybridWebViewHandler`, Shell query attributes, Hot Reload helpers
and `VisualTypeConverter`.
Both Release apks ran on the emulator.
In each, an item was added, an item checked and dark mode switched on.

## Known gaps

- The library's mobile target frameworks stay opt-in (`-p:PureMarkupMobile=true`).
  The release workflow installs only `maui-windows`.
- Not tested on a physical Android device, and not on arm64.
- iOS and Mac Catalyst still need a Mac to build an app bundle and to run.
  The NativeAOT checks are listed in [trimming-aot.md](trimming-aot.md).
- Hot Reload was not tested on Android.
- In `ScreenshotPage`, which has no Shell, the status bar draws edge to edge.
  Its icons do not follow the in-app theme toggle.
  This is standard MAUI behavior; PureMarkup does not cause it.

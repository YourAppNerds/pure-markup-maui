# Getting started

This page takes you from an empty folder to a running app with your own page.

## What you need

- The .NET 10 SDK.
- The .NET MAUI workload for each platform you build for.
- Windows, for the Windows target.

## 1. Get the packages

PureMarkup.Maui is on NuGet as a preview:

- `PureMarkup.Maui`: the library and its source generator.
- `PureMarkup.Maui.Templates`: the `dotnet new` templates.

You need nothing else for this step.
To use a build of this repo instead, see [Use a local build](#use-a-local-build).

## 2. Install the templates

```
dotnet new install PureMarkup.Maui.Templates::0.1.0-preview.1
```

This adds two templates:

| Short name | What it makes |
|---|---|
| `puremarkup-app` | A .NET MAUI app with no XAML files, including the Windows host. |
| `puremarkup-page` | One `ContentPage` written in C# markup. |

## 3. Create the app

```
dotnet new puremarkup-app -n MyApp --no-restore
cd MyApp
```

`--no-restore` skips the restore that the template would run.
On a machine without every mobile workload, that restore fails (see step 4).

### Options

| Option | Default | Effect |
|---|---|---|
| `--framework net10.0\|net11.0` | `net10.0` | Target framework. `net11.0` needs the .NET 11 SDK and is not built or tested yet. |
| `--applicationId <id>` | `com.companyname.<name>` | The app's `ApplicationId`. |
| `--sample-content`, `-sc` | off | Adds a Todo tab: an enum-named grid, two-way bindings and a C# item template. |
| `--no-windows` | off | Drops the Windows target framework and `Platforms/Windows`. |
| `--puremarkup-version <v>` | `0.1.0-preview.1` | The PureMarkup.Maui package version the app references. |
| `--no-restore` | off | Skips the restore after creation. |

## 4. Build and run

The app lists the mobile target frameworks (Android, iOS, Mac Catalyst) next to Windows.
Restore needs a workload for every one of them.
If one is missing, restore fails with this error:

```
error NETSDK1147: To build this project, the following workloads must be installed: ...
```

This is a known gap.
You have two ways around it.

**Install the workloads.**
Run `dotnet workload restore` in the app folder.
After that, a normal `dotnet build` works.

**Build only for Windows.**
Override the target frameworks on the command line:

```
dotnet build -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0
dotnet build -t:Run -f net10.0-windows10.0.19041.0 -p:TargetFrameworks=net10.0-windows10.0.19041.0
```

The first line builds.
The second line builds and starts the app.
With `--framework net11.0`, use `net11.0-windows10.0.19041.0` instead.

## What is in the app

| File | Role |
|---|---|
| `MauiProgram.cs` | Registers fonts, pages and view models. |
| `App.cs` | Turns on the stock MAUI look with `this.UsePresets(r => r.UseDefaultLook())`. |
| `AppShell.cs` | The Shell and its routes. |
| `Pages/MainPage.cs` | The counter page from the stock MAUI template, in C# markup. |
| `ViewModels/MainViewModel.cs` | Its view model, written with CommunityToolkit.Mvvm. |
| `Resources/Theme/Tokens.cs` | Your theme tokens (light and dark values). |
| `Resources/Theme/AppPresets.cs` | Your presets, derived from the stock look. |
| `Platforms/Windows/App.cs`, `Program.cs` | The Windows host in C#, with no `App.xaml`. |
| `GlobalUsings.cs` | `PureMarkup.Maui`, `PureMarkup.Maui.Defaults` and your app's namespaces. |

The project sets `IsAotCompatible`.
So the trim and AOT analyzers check your own code too.

## Your first page

Add a page with the page template.
Pass `--namespace` so the page lands in the same namespace as `MainPage`:

```
dotnet new puremarkup-page -n ProfilePage -o Pages --namespace MyApp.Pages
```

Without `--namespace`, the page gets the project's root namespace (`MyApp`).

Add a view model in `ViewModels/ProfileViewModel.cs`:

```csharp
namespace MyApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Name { get; set; } = "";

	[RelayCommand]
	void Save() => SemanticScreenReader.Announce($"Saved {Name}");
}
```

Replace the body of `Pages/ProfilePage.cs`:

```csharp
namespace MyApp.Pages;

public class ProfilePage : ContentPage
{
	public ProfilePage(ProfileViewModel vm)
	{
		this.BindingContext(vm).Title("Profile").Padding(24)
			.Content(new VerticalStackLayout
			{
				Children =
				{
					DefaultLook.Headline.New().Text("Profile"),

					new Entry().Placeholder("Name")
						.Text(Binding.Create(static (ProfileViewModel m) => m.Name, BindingMode.TwoWay)),

					new Label()
						.Text(Binding.Create(static (ProfileViewModel m) => m.Name, stringFormat: "Hello, {0}!")),

					AppPresets.PrimaryButton.New().Text("Save")
						.Command(vm.SaveCommand)
						.FillHorizontal(),
				},
			}.Spacing(12));
	}
}
```

Register both in `MauiProgram.cs`, next to `MainPage`:

```csharp
builder.Services.AddTransient<ProfilePage>();
builder.Services.AddTransient<ProfileViewModel>();
```

Show the page as a tab in `AppShell.cs`:

```csharp
Items.Add(new TabBar
{
	Items =
	{
		new ShellContent().Title("Home").Route("MainPage")
			.ContentTemplate(new DataTemplate(typeof(MainPage))),
		new ShellContent().Title("Profile").Route("ProfilePage")
			.ContentTemplate(new DataTemplate(typeof(ProfilePage))),
	},
});
```

Shell creates the page through the service provider.
So the constructor gets its view model.

## Next

- [Markup API](markup-api.md): how methods are named, bindings, layouts.
- [Presets and theming](presets-theming.md): shared looks, tokens, dark mode.
- [Diagnostics](diagnostics.md): what each `PMxxxx` message means.

## Use a local build

To try changes from this repo, build both packages in the repo root:

```
dotnet pack src/PureMarkup.Maui -c Release -o artifacts/packages
dotnet pack templates/PureMarkup.Maui.Templates -c Release -o artifacts/packages
```

Install the templates from the file:

```
dotnet new install artifacts/packages/PureMarkup.Maui.Templates.0.1.0-preview.1.nupkg
```

The app references `PureMarkup.Maui` 0.1.0-preview.1.
Add your `artifacts/packages` folder as a package source.
Add a `nuget.config` next to `MyApp.csproj`.
Use the full path to your clone:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="puremarkup-local" value="C:\path\to\pure-markup-maui\artifacts\packages" />
  </packageSources>
</configuration>
```

Your build has the same version as the NuGet package.
NuGet keeps using whichever copy it cached first.
After each repack, delete the `puremarkup.maui/0.1.0-preview.1` folder from your global packages folder.
`dotnet nuget locals global-packages --list` shows where that folder is.


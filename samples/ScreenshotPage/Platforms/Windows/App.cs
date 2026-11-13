using Microsoft.UI.Xaml.Markup;

namespace ScreenshotPage.WinUI;

/// <summary>
/// C#-only WinUI host: replaces Platforms/Windows/App.xaml + App.xaml.cs. Without App.xaml the
/// XAML compiler does not generate the IXamlMetadataProvider part, so this class forwards to the
/// providers of WinUI controls, MAUI Controls and MAUI Core (see spikes/windows-no-appxaml).
/// </summary>
public class App : MauiWinUIApplication, IXamlMetadataProvider
{
    readonly IXamlMetadataProvider[] _providers =
    [
        new Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider(),
        new Microsoft.Maui.Controls.Controls_Core_XamlTypeInfo.XamlMetaDataProvider(),
        new Microsoft.Maui.Core_XamlTypeInfo.XamlMetaDataProvider(),
    ];

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public IXamlType? GetXamlType(Type type) => Find(p => p.GetXamlType(type));

    public IXamlType? GetXamlType(string fullName) => Find(p => p.GetXamlType(fullName));

    public XmlnsDefinition[] GetXmlnsDefinitions() =>
        _providers.SelectMany(p => p.GetXmlnsDefinitions()).ToArray();

    // Same rule as the generated code: prefer a constructible type, else the first stub found.
    IXamlType? Find(Func<IXamlMetadataProvider, IXamlType?> lookup)
    {
        IXamlType? stub = null;
        foreach (var provider in _providers)
        {
            var type = lookup(provider);
            if (type is null) continue;
            if (type.IsConstructible) return type;
            stub ??= type;
        }
        return stub;
    }
}

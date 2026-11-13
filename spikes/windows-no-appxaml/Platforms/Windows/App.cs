using Microsoft.UI.Xaml.Markup;

namespace WindowsNoAppXaml.WinUI;

/// <summary>
/// C#-only WinUI host. Replaces the template's Platforms/Windows/App.xaml + App.xaml.cs.
/// The XAML compiler normally generates the IXamlMetadataProvider part of this class
/// (XamlTypeInfo.g.cs); without App.xaml it does not, so we forward to the providers of
/// the referenced libraries (WinUI controls, MAUI Controls, MAUI Core) by hand.
/// </summary>
public class App : MauiWinUIApplication, IXamlMetadataProvider
{
    readonly IXamlMetadataProvider[] _providers =
    [
        new Microsoft.UI.Xaml.XamlTypeInfo.XamlControlsXamlMetaDataProvider(),
        new Microsoft.Maui.Controls.Controls_Core_XamlTypeInfo.XamlMetaDataProvider(),
        new Microsoft.Maui.Core_XamlTypeInfo.XamlMetaDataProvider(),
    ];

    public App()
    {
        UnhandledException += (_, e) =>
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "WindowsNoAppXaml.log"), $"{DateTime.Now:O} {e.Exception}{Environment.NewLine}");
    }

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

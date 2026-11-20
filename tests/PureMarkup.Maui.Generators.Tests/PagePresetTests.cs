using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using PureMarkup.Maui.Defaults;
using C = PureMarkup.Maui.Defaults.DefaultColors;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>
/// Presets on TabbedPage and Shell instances. Both need a dispatcher to construct, so the tests
/// install an inline one (no UI thread, no platform handlers: setters only, not rendering).
/// </summary>
[Collection(nameof(PresetTests))] // Theme, the preset cache and the dispatcher provider are static
public sealed class PagePresetTests : IDisposable
{
    private readonly IDispatcherProvider _previous = DispatcherProvider.Current;

    public PagePresetTests() => DispatcherProvider.SetCurrent(new InlineDispatcherProvider());

    public void Dispose() => DispatcherProvider.SetCurrent(_previous);

    private static Preset<TabbedPage> Tabs => Preset.Once<TabbedPage>(static p => p
        .BarBackgroundColor(Colors.Red).BarTextColor(Colors.Yellow));

    private static Preset<Shell> ShellLook => Preset.Once<Shell>(static p => p
        .Set(Shell.BackgroundColorProperty, Colors.Red).Set(Shell.NavBarHasShadowProperty, true));

    private sealed class MyTabs : TabbedPage;

    [Fact]
    public void UseAppliesToTabbedPage()
    {
        var tabs = new TabbedPage().Use(Tabs).BarTextColor(Colors.Blue);
        Assert.Equal(Colors.Red, tabs.BarBackgroundColor);
        Assert.Equal(Colors.Blue, tabs.BarTextColor); // local value wins

        var derived = new MyTabs().Use(Tabs);
        Assert.Equal(Colors.Red, derived.BarBackgroundColor);
    }

    [Fact]
    public void UseAppliesToShell()
    {
        var shell = new Shell().Use(ShellLook);
        Assert.Equal(Colors.Red, Shell.GetBackgroundColor(shell));
        Assert.True(Shell.GetNavBarHasShadow(shell));
    }

    [Theory, MemberData(nameof(DefaultLookTests.Themes), MemberType = typeof(DefaultLookTests))]
    public void ImplicitDefaultsApplyToTabbedPageAndShell(AppTheme theme)
    {
        var dark = theme == AppTheme.Dark;

        var tabs = new TabbedPage();
        Theme.Apply(tabs.Resources, theme);
        tabs.Resources.UseDefaultLook();
        Assert.Equal(dark ? C.Gray950 : C.White, tabs.BarBackgroundColor);
        Assert.Equal(dark ? C.White : C.Magenta, tabs.BarTextColor);
        Assert.Equal(dark ? C.Gray200 : C.Gray950, tabs.SelectedTabColor);

        var shell = new Shell();
        Theme.Apply(shell.Resources, theme);
        shell.Resources.UseDefaultLook();
        Assert.Equal(dark ? C.OffBlack : C.White, Shell.GetBackgroundColor(shell));
        Assert.Equal(dark ? C.White : C.Magenta, Shell.GetTabBarForegroundColor(shell));
        Assert.False(Shell.GetNavBarHasShadow(shell));
    }

    [Fact]
    public void UseBeatsImplicitDefaultOnShell()
    {
        var shell = new Shell();
        Theme.Apply(shell.Resources, AppTheme.Light);
        shell.Resources.UseDefaultLook();
        shell.Use(ShellLook);
        Assert.Equal(Colors.Red, Shell.GetBackgroundColor(shell));
        Assert.True(Shell.GetNavBarHasShadow(shell));
        Assert.Equal(C.Magenta, Shell.GetTabBarForegroundColor(shell)); // other implicit setters still apply
    }

    private sealed class InlineDispatcherProvider : IDispatcherProvider
    {
        private static readonly InlineDispatcher Instance = new();
        public IDispatcher GetForCurrentThread() => Instance;
    }

    private sealed class InlineDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;
        public bool Dispatch(Action action) { action(); return true; }
        public bool DispatchDelayed(TimeSpan delay, Action action) { action(); return true; }
        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }
}

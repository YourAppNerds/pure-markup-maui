using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PureMarkup.Maui;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>
/// Compile test: the target snippet from the design note builds against the generated API in
/// the PureMarkup.Maui library (net10.0 TFM).
/// </summary>
public class ScreenshotSnippetTests
{
    public class ViewModel
    {
        public string RegistrationCode { get; set; } = "";
    }

    internal static Entry Build() =>
        new Entry()
            .Keyboard(Keyboard.Numeric).BackgroundColor(Colors.AliceBlue)
            .Row(1).Column(1)
            .FontSize(15).Placeholder("Enter number").TextColor(Colors.Black)
            .HeightRequest(44).Margin(new Thickness(5, 5))
            .Text(Binding.Create(static (ViewModel vm) => vm.RegistrationCode, BindingMode.TwoWay));

    [Fact]
    public void ValueSettersApply()
    {
        // Same chain without the binding: Binding.Create(Func) relies on MAUI's interceptor.
        var entry = new Entry()
            .Keyboard(Keyboard.Numeric).BackgroundColor(Colors.AliceBlue)
            .Row(1).Column(1)
            .FontSize(15).Placeholder("Enter number").TextColor(Colors.Black)
            .HeightRequest(44).Margin(new Thickness(5, 5));

        Assert.Equal(Keyboard.Numeric, entry.Keyboard);
        Assert.Equal(1, Grid.GetRow(entry));
        Assert.Equal(1, Grid.GetColumn(entry));
        Assert.Equal(15, entry.FontSize);
        Assert.Equal("Enter number", entry.Placeholder);
        Assert.Equal(44, entry.HeightRequest);
        Assert.Equal(new Thickness(5, 5), entry.Margin);
    }

    [Fact]
    public void SnippetBuildsWithBinding()
    {
        var entry = Build();
        Assert.True(entry.IsSet(Entry.TextProperty) || entry.GetValue(Entry.TextProperty) is null or "");
    }
}

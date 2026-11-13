using System.ComponentModel;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>
/// Presets on real MAUI objects, headless. Mirrors the precedence table in design.md section 3:
/// default &lt; implicit style &lt; BasedOn base &lt; explicit style &lt; local value &lt; trigger &lt; visual state.
/// </summary>
[Collection(nameof(PresetTests))] // Theme and the preset cache are static
public class PresetTests
{
    private static readonly Token<Color> Text = new(Colors.Black, Colors.White, "Text");

    private static Preset<Entry> Field => Preset.Once<Entry>(static p => p
        .FontSize(15).Height(44).Margin(5, 5)
        .BackgroundColor(Colors.White)
        .State(VisualStateManager.CommonStates.Disabled, static s => s.BackgroundColor(Colors.Gray)));

    private static Preset<Entry> NumericField => Field.With(static p => p
        .Keyboard(Keyboard.Numeric).FontSize(20)
        .State(VisualStateManager.CommonStates.Focused, static s => s.FontSize(30)));

    [Fact]
    public void PresetValuesApply()
    {
        var entry = new Entry().Use(Field);
        Assert.Equal(15, entry.FontSize);
        Assert.Equal(44, entry.HeightRequest);
        Assert.Equal(new Thickness(5, 5), entry.Margin);
    }

    [Fact]
    public void LocalValueBeatsPresetInAnyOrder()
    {
        var after = new Entry().Use(Field).FontSize(18);
        var before = new Entry().FontSize(18).Use(Field);
        Assert.Equal(18, after.FontSize);
        Assert.Equal(18, before.FontSize);
        Assert.Equal(44, before.HeightRequest); // untouched preset values still apply
    }

    [Fact]
    public void VisualStateBeatsLocalValue()
    {
        var entry = new Entry().Use(Field).BackgroundColor(Colors.Red);
        Assert.Equal(Colors.Red, entry.BackgroundColor);

        entry.IsEnabled = false;
        Assert.Equal(Colors.Gray, entry.BackgroundColor);

        entry.IsEnabled = true;
        Assert.Equal(Colors.Red, entry.BackgroundColor);
    }

    [Fact]
    public void TriggerBeatsLocalValue()
    {
        var preset = Preset.Once<Entry>(static p => p
            .Trigger(InputView.IsReadOnlyProperty, true, static t => t.BackgroundColor(Colors.LightGray)));
        var entry = new Entry().Use(preset).BackgroundColor(Colors.Red);
        Assert.Equal(Colors.Red, entry.BackgroundColor);

        entry.IsReadOnly = true;
        Assert.Equal(Colors.LightGray, entry.BackgroundColor);

        entry.IsReadOnly = false;
        Assert.Equal(Colors.Red, entry.BackgroundColor);
    }

    [Fact]
    public void DataTriggerApplies()
    {
        var preset = Preset.Once<Label>(static p => p
            .DataTrigger(Binding.Create(static (Flag f) => f.On), true, static t => t.TextColor(Colors.Green)));
        // Headless there is no dispatcher for PropertyChanged, so check the initial evaluation.
        Assert.NotEqual(Colors.Green, new Label().Use(preset).BindingContext(new Flag()).TextColor);
        Assert.Equal(Colors.Green, new Label().Use(preset).BindingContext(new Flag { On = true }).TextColor);
    }

    [Fact]
    public void WithDerivedBeatsBase()
    {
        var entry = new Entry().Use(NumericField);
        Assert.Equal(20, entry.FontSize);
        Assert.Equal(Keyboard.Numeric, entry.Keyboard);
        Assert.Equal(44, entry.HeightRequest);
        Assert.Same(Field.Style, NumericField.Style.BasedOn);
    }

    [Fact]
    public void WithMergesVisualStates()
    {
        var entry = new Entry().Use(NumericField);
        entry.IsEnabled = false;
        Assert.Equal(Colors.Gray, entry.BackgroundColor); // base preset's Disabled state survives
        entry.IsEnabled = true;
        Assert.Equal(Colors.White, entry.BackgroundColor);
    }

    [Fact]
    public void ImplicitDefaultAppliesToPlainElement()
    {
        var page = new ContentPage();
        page.Resources.Default(Field);
        var entry = new Entry();
        page.Content = entry;
        Assert.Equal(15, entry.FontSize);
        Assert.Equal(44, entry.HeightRequest);
    }

    [Fact]
    public void UseBeatsImplicitDefault()
    {
        var danger = Preset.Once<Entry>(static p => p.FontSize(22).TextColor(Colors.Red));
        var page = new ContentPage();
        page.Resources.Default(Field);
        var entry = new Entry().Use(danger);
        page.Content = entry;
        Assert.Equal(22, entry.FontSize);           // explicit beats implicit
        Assert.Equal(Colors.Red, entry.TextColor);
        Assert.Equal(44, entry.HeightRequest);      // MAUI merges: implicit setters underneath still apply
    }

    [Fact]
    public void SecondUseReplacesFirst()
    {
        var big = Preset.Once<Entry>(static p => p.FontSize(40));
        var entry = new Entry().Use(Field).Use(big);
        Assert.Equal(40, entry.FontSize);
        Assert.NotEqual(44, entry.HeightRequest);
    }

    [Fact]
    public void TokenFollowsThemeInPresetAndLocally()
    {
        var preset = Preset.Once<Entry>(static p => p.TextColor(Text));
        var page = new ContentPage();
        Theme.Apply(page.Resources, AppTheme.Light);
        var entry = new Entry().Use(preset);
        var label = new Label().TextColor(Text);
        page.Content = new VerticalStackLayout { entry, label };
        Assert.Equal(Colors.Black, entry.TextColor);
        Assert.Equal(Colors.Black, label.TextColor);

        Theme.Apply(page.Resources, AppTheme.Dark);
        Assert.Equal(Colors.White, entry.TextColor);
        Assert.Equal(Colors.White, label.TextColor);
    }

    [Fact]
    public void TokenCreatedAfterApplyIsWritten()
    {
        var page = new ContentPage();
        Theme.Apply(page.Resources, AppTheme.Dark);
        var late = new Token<double>(12, 14, "Late");
        Assert.Equal(14d, page.Resources[late.Key]);
    }

    [Fact]
    public void BindingInPreset()
    {
        var preset = Preset.Once<Label>(static p => p.Text(Binding.Create(static (Flag f) => f.Name)));
        var label = new Label().Use(preset).BindingContext(new Flag { Name = "bound" });
        Assert.Equal("bound", label.Text);
    }

    [Fact]
    public void AlsoRunsOnUseAndNew()
    {
        var clicks = 0;
        var preset = Preset.Once<Button>(static p => p.Text("Go").Also(static b => b.AutomationId = "go"));
        var used = new Button().Use(preset);
        var made = preset.New().OnClicked(() => clicks++);
        Assert.Equal(("go", "go"), (used.AutomationId, made.AutomationId));
        Assert.Equal("Go", made.Text);
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void UseAcceptsDerivedElementType()
    {
        var entry = new MyEntry().Use(Field);
        Assert.Equal(15, entry.FontSize);
    }

    [Fact]
    public void StyleIsCachedAndHotReloadClearsIt()
    {
        var first = Field.Style;
        Assert.Same(first, Field.Style);
        Assert.True(PresetCache.Count > 0);

        PresetHotReload.ClearCache(null);
        Assert.Equal(0, PresetCache.Count);
        Assert.NotSame(first, Field.Style);
    }

    private sealed class MyEntry : Entry;

    internal sealed class Flag : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; } = "";

        public bool On
        {
            get;
            set
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(On)));
            }
        }
    }
}

using BenchmarkDotNet.Attributes;
using Microsoft.Maui.Controls;

namespace PageConstruction.Bench;

/// <summary>
/// One registration form (title, four label + entry rows, two buttons, a status label, a 7x2 grid)
/// built five ways. Headless: no Application, no handlers, no platform views, so this measures the
/// object graph, BindableProperty writes and Style application, not layout or rendering.
/// </summary>
[MemoryDiagnoser]
public class PageBenchmarks
{
    [GlobalSetup]
    public void Setup() => Equivalence.Check();

    [Benchmark(Baseline = true, Description = "Plain C# setters")]
    public ContentPage Plain() => PlainPage.Build();

    [Benchmark(Description = "PureMarkup fluent")]
    public ContentPage PureMarkup() => PureMarkupPage.Build();

    [Benchmark(Description = "CommunityToolkit.Maui.Markup")]
    public ContentPage Toolkit() => ToolkitPage.Build();

    [Benchmark(Description = "Plain C# + shared Styles")]
    public ContentPage PlainStyles() => PlainStylesPage.Build();

    [Benchmark(Description = "PureMarkup + presets (.Use)")]
    public ContentPage Presets() => PresetsPage.Build();
}

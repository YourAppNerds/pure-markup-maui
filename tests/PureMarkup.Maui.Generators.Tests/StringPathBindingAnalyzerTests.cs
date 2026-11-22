using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>PM0007 against the real MAUI and PureMarkup.Maui references.</summary>
public class StringPathBindingAnalyzerTests
{
    private static async Task<ImmutableArray<Diagnostic>> Analyze(string body)
    {
        var source = $$"""
            using Microsoft.Maui.Controls;
            using PureMarkup.Maui;
            public class VM { public string Name { get; set; } = ""; }
            public static class Page
            {
                public static void Build()
                {
                    {{body}}
                }
            }
            """;
        var references = TestCompilation.WithMaui
            .Append(MetadataReference.CreateFromFile(typeof(LabelMarkup).Assembly.Location));
        var compilation = TestCompilation.Create(source, references);
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
        return await compilation
            .WithAnalyzers([new StringPathBindingAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    [Theory]
    [InlineData("""new Label().Text(new Binding("Name"));""")]
    [InlineData("""new Label().Text(new Binding { Path = "Name" });""")]
    [InlineData("""var b = new Binding("Name"); new Label().Text(b);""")]
    [InlineData("""Preset.Once<Label>(p => p.DataTrigger(new Binding("Name"), "x", s => s.Opacity(0.5)));""")]
    [InlineData("""BindingBase b = new Binding("Name"); new Label().Text(b);""")]
    [InlineData("""BindingBase b = Binding.Create(static (VM vm) => vm.Name); b = new Binding("Name"); new Label().Text(b);""")]
    [InlineData("""BindingBase a = new Binding("Name"); var b = a; new Label().Text(b);""")]
    [InlineData("""BindingBase b = true ? new Binding("Name") : Binding.Create(static (VM vm) => vm.Name); new Label().Text(b);""")]
    [InlineData("""new Label().Text(new MultiBinding { Bindings = { new Binding("Name"), Binding.Create(static (VM vm) => vm.Name) }, StringFormat = "{0} {1}" });""")]
    [InlineData("""new Label().Text(new MultiBinding { Bindings = new System.Collections.Generic.List<BindingBase> { new Binding("Name") } });""")]
    [InlineData("""new Label().Text(new MultiBinding { Bindings = [new Binding("Name")] });""")]
    [InlineData("""BindingBase child = new Binding("Name"); var m = new MultiBinding { Bindings = { child } }; new Label().Text(m);""")]
    [InlineData("""var m = new MultiBinding(); m.Bindings.Add(new Binding("Name")); new Label().Text(m);""")]
    public async Task StringPathIsReported(string body)
    {
        var diagnostic = Assert.Single(await Analyze(body));
        Assert.Equal("PM0007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
    }

    [Theory]
    [InlineData("""new Label().Text(Binding.Create(static (VM vm) => vm.Name));""")]
    [InlineData("""new Label().Text("Name");""")]
    [InlineData("""new Label().SetBinding(Label.TextProperty, new Binding("Name"));""")]
    [InlineData("""BindingBase b = Binding.Create(static (VM vm) => vm.Name); new Label().Text(b);""")]
    [InlineData("""BindingBase b = Binding.Create(static (VM vm) => vm.Name); BindingBase other = new Binding("Name"); new Label().Text(b);""")]
    [InlineData("""new Label().Text(new MultiBinding { Bindings = { Binding.Create(static (VM vm) => vm.Name) }, StringFormat = "{0}" });""")]
    [InlineData("""var m = new MultiBinding(); m.Bindings.Add(Binding.Create(static (VM vm) => vm.Name)); new Label().Text(m);""")]
    public async Task OtherCallsAreNotReported(string body) =>
        Assert.Empty(await Analyze(body));
}

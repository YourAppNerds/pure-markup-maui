using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit.Abstractions;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>Runs the generator against the real Microsoft.Maui.Controls reference, as the library build does.</summary>
public class RealMauiTests(ITestOutputHelper output)
{
    private const string Source = """
        [assembly: PureMarkup.Maui.GenerateMarkup(typeof(Microsoft.Maui.Controls.View), WholeAssembly = true)]
        """;

    private static readonly Lazy<(GeneratorDriverRunResult Result, Compilation Output, IReadOnlyList<Diagnostic> Diagnostics)> Run = new(() =>
    {
        var (driver, compilation, diagnostics) = TestCompilation.Run(TestCompilation.Create(Source, TestCompilation.WithMaui));
        return (driver.GetRunResult(), compilation, diagnostics);
    });

    private static string AllText => string.Join("\n", Run.Value.Result.GeneratedTrees.Select(t => t.ToString()));

    [Fact]
    public void GeneratedCodeCompiles()
    {
        var errors = Run.Value.Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join("\n", errors.Take(20)));
    }

    [Fact]
    public void CountsAreInExpectedRange()
    {
        var methods = Regex.Matches(AllText, @"^    public static ", RegexOptions.Multiline).Count;
        var classes = Run.Value.Result.GeneratedTrees.Length - 1;
        output.WriteLine($"classes: {classes}, methods: {methods}");
        foreach (var group in Run.Value.Diagnostics.GroupBy(d => d.Id).OrderBy(g => g.Key))
        {
            output.WriteLine($"{group.Key}: {group.Count()}");
        }

        foreach (var d in Run.Value.Diagnostics.Where(d => d.Id is "PM0003" or "PM0005"))
        {
            output.WriteLine(d.GetMessage());
        }

        Assert.InRange(classes, 100, 400);
        Assert.InRange(methods, 1000, 5000);
        Assert.DoesNotContain(Run.Value.Diagnostics, d => d.Severity >= DiagnosticSeverity.Warning);
    }

    [Theory]
    [InlineData("public static T Keyboard<T>(this T self, global::Microsoft.Maui.Keyboard? value) where T : global::Microsoft.Maui.Controls.InputView")]
    [InlineData("public static T Keyboard<T>(this T self, global::Microsoft.Maui.Controls.BindingBase binding) where T : global::Microsoft.Maui.Controls.InputView")]
    [InlineData("self.SetValue(global::Microsoft.Maui.Controls.InputView.KeyboardProperty, value);")]
    [InlineData("public static T HeightRequest<T>(this T self, double value) where T : global::Microsoft.Maui.Controls.VisualElement")]
    [InlineData("public static T Row<T>(this T self, int value) where T : global::Microsoft.Maui.Controls.BindableObject")]
    [InlineData("global::Microsoft.Maui.Controls.Grid.SetRow(self, value);")]
    [InlineData("public static T OnClicked<T>(this T self, global::System.EventHandler handler) where T : global::Microsoft.Maui.Controls.Button")]
    [InlineData("public static T OnClicked<T>(this T self, global::System.Action handler) where T : global::Microsoft.Maui.Controls.Button")]
    public void SpotChecks(string expected) => Assert.Contains(expected, AllText);
}

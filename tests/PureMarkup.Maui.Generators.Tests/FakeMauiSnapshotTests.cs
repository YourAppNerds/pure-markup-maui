using Microsoft.CodeAnalysis;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>Snapshot tests over a tiny hand-written stand-in for Microsoft.Maui.Controls.</summary>
public class FakeMauiSnapshotTests
{
    private const string FakeMaui = """
        using System;

        [assembly: PureMarkup.Maui.GenerateMarkup(typeof(Microsoft.Maui.Controls.View), WholeAssembly = true)]

        namespace Microsoft.Maui.Controls
        {
            public sealed class BindableProperty { }
            public abstract class BindingBase { }
            public class BindableObject
            {
                public void SetValue(BindableProperty property, object? value) { }
                public void SetBinding(BindableProperty property, BindingBase binding) { }
            }

            public enum Keyboard { Default, Numeric }

            public class VisualElement : BindableObject
            {
                public static readonly BindableProperty HeightRequestProperty = new();
                public double HeightRequest { get; set; }

                public static readonly BindableProperty BackgroundColorProperty = new();
                public string? BackgroundColor { get; set; }

                public static readonly BindableProperty IsFocusedProperty = new();
                public bool IsFocused { get; }

                public event EventHandler? Focused;
            }

            public class View : VisualElement
            {
                public object? Tag { get; set; }
                public Func<int>? Callback { get; set; }
            }

            public class InputView : View
            {
                public static readonly BindableProperty KeyboardProperty = new();
                public Keyboard Keyboard { get; set; }

                public static readonly BindableProperty TextProperty = new();
                public string? Text { get; set; }
            }

            public class Entry : InputView { }

            public sealed class Label : View
            {
                public static readonly BindableProperty TextProperty = new();
                public string? Text { get; set; }
            }

            public class Button : View
            {
                public event EventHandler? Clicked;
            }

            public class Grid : View
            {
                public static readonly BindableProperty RowProperty = new();
                public static void SetRow(BindableObject bindable, int value) { }
                public static int GetRow(BindableObject bindable) => 0;
            }

            public static class ToolTipProperties
            {
                public static readonly BindableProperty TextProperty = new();
                public static void SetText(BindableObject bindable, object value) { }
            }

            public class Shell : View
            {
                public static readonly BindableProperty BackgroundColorProperty = new();
                public static void SetBackgroundColor(BindableObject obj, string? value) { }
            }

            public class ItemsView<TItem> : View
            {
                public static readonly BindableProperty ItemProperty = new();
                public TItem? Item { get; set; }
            }

            public class Hidden : View
            {
                [Obsolete("old")]
                public int Old { get; set; }
            }
        }
        """;

    [Fact]
    public Task GeneratesExpectedSource()
    {
        var compilation = TestCompilation.Create(FakeMaui, TestCompilation.Runtime);
        var (driver, output, generatorDiagnostics) = TestCompilation.Run(compilation);

        Assert.Empty(generatorDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));

        return Verify(driver);
    }

    [Fact]
    public void ReportsSkips()
    {
        var compilation = TestCompilation.Create(FakeMaui, TestCompilation.Runtime);
        var (_, _, diagnostics) = TestCompilation.Run(compilation);

        var ids = diagnostics.Select(d => d.Id).OrderBy(id => id).ToArray();
        Assert.Equal(["PM0001", "PM0002", "PM0004"], ids);
    }

    [Fact]
    public void NoAttributeGeneratesOnlyTheAttribute()
    {
        var compilation = TestCompilation.Create(FakeMaui.Replace("[assembly:", "// [assembly:"), TestCompilation.Runtime);
        var (driver, _, diagnostics) = TestCompilation.Run(compilation);

        Assert.Empty(diagnostics);
        var tree = Assert.Single(driver.GetRunResult().GeneratedTrees);
        Assert.EndsWith("GenerateMarkupAttribute.g.cs", tree.FilePath);
    }

    [Fact]
    public void SingleTypeModeGeneratesOnlyThatType()
    {
        var source = FakeMaui.Replace(
            "typeof(Microsoft.Maui.Controls.View), WholeAssembly = true",
            "typeof(Microsoft.Maui.Controls.Button), Namespace = \"MyApp.Markup\"");
        var (driver, output, _) = TestCompilation.Run(TestCompilation.Create(source, TestCompilation.Runtime));

        Assert.Empty(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = driver.GetRunResult().GeneratedTrees.Select(t => Path.GetFileName(t.FilePath)).OrderBy(n => n).ToArray();
        Assert.Equal(["ButtonMarkup.g.cs", "GenerateMarkupAttribute.g.cs"], generated);
        var text = driver.GetRunResult().GeneratedTrees.Single(t => t.FilePath.EndsWith("ButtonMarkup.g.cs")).ToString();
        Assert.Contains("namespace MyApp.Markup;", text);
    }
}

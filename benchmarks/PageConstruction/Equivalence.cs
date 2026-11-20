using Microsoft.Maui.Controls;

namespace PageConstruction.Bench;

/// <summary>Fails fast if a variant drifts from the plain page (effective values, grid cells).</summary>
internal static class Equivalence
{
    public static void Check()
    {
        var expected = Describe(PlainPage.Build());
        foreach (var (name, build) in new (string, Func<ContentPage>)[]
        {
            ("PureMarkup", PureMarkupPage.Build), ("Toolkit", ToolkitPage.Build),
            ("PlainStyles", PlainStylesPage.Build), ("Presets", PresetsPage.Build),
        })
        {
            var actual = Describe(build());
            if (actual != expected)
            {
                throw new InvalidOperationException($"{name} differs from Plain:\n{actual}\n--- expected ---\n{expected}");
            }
        }
    }

    private static string Describe(ContentPage page)
    {
        var grid = (Grid)page.Content;
        var lines = new List<string>
        {
            $"page padding={page.Padding} rows={grid.RowDefinitions.Count} cols={grid.ColumnDefinitions.Count} spacing={grid.RowSpacing}/{grid.ColumnSpacing}",
        };
        foreach (var child in grid.Children.Cast<View>())
        {
            var cell = $"r{Grid.GetRow(child)}c{Grid.GetColumn(child)}s{Grid.GetColumnSpan(child)} h={child.HeightRequest} m={child.Margin} bg={child.BackgroundColor?.ToArgbHex()}";
            lines.Add(child switch
            {
                Label l => $"Label {cell} '{l.Text}' {l.FontSize} {l.FontAttributes} {l.VerticalTextAlignment}",
                Entry e => $"Entry {cell} '{e.Placeholder}' {e.Keyboard} {e.FontSize} {e.TextColor?.ToArgbHex()}",
                Button b => $"Button {cell} '{b.Text}' {b.FontSize} {b.TextColor?.ToArgbHex()} {b.CornerRadius}",
                _ => child.GetType().Name,
            });
        }

        return string.Join("\n", lines);
    }
}

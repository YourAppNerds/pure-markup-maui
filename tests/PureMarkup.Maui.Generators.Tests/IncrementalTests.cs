using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>Edits to user source must not re-run the whole-assembly walk over Microsoft.Maui.Controls.</summary>
public class IncrementalTests
{
    private const string Source = """
        [assembly: PureMarkup.Maui.GenerateMarkup(typeof(Microsoft.Maui.Controls.View), WholeAssembly = true)]

        public class UserCode { public int Value => 1; }
        """;

    [Fact]
    public void SourceEditKeepsWholeAssemblyWalkCached()
    {
        var compilation = TestCompilation.Create(Source, TestCompilation.WithMaui);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MarkupGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        var first = driver.GetRunResult().Results.Single();
        Assert.Equal(IncrementalStepRunReason.New, WalkReason(first));
        Assert.True(first.GeneratedSources.Length > 100);

        var oldTree = compilation.SyntaxTrees.Single();
        var newTree = oldTree.WithChangedText(Microsoft.CodeAnalysis.Text.SourceText.From(
            Source.Replace("Value => 1", "Value => 2") + "\npublic class MoreUserCode { }\n"));
        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(oldTree, newTree));
        var second = driver.GetRunResult().Results.Single();

        Assert.Equal(IncrementalStepRunReason.Cached, WalkReason(second));
        Assert.All(second.TrackedSteps[TrackingNames.WholeAssemblyKey].SelectMany(s => s.Outputs),
            o => Assert.Equal(IncrementalStepRunReason.Unchanged, o.Reason));
        Assert.All(second.TrackedOutputSteps.SelectMany(s => s.Value).SelectMany(s => s.Outputs),
            o => Assert.True(o.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged, o.Reason.ToString()));
        Assert.Equal(first.GeneratedSources.Length, second.GeneratedSources.Length);
    }

    [Fact]
    public void AttributeEditReRunsWalk()
    {
        var compilation = TestCompilation.Create(Source, TestCompilation.WithMaui);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MarkupGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);

        var oldTree = compilation.SyntaxTrees.Single();
        var newTree = oldTree.WithChangedText(Microsoft.CodeAnalysis.Text.SourceText.From(
            Source.Replace("WholeAssembly = true", "WholeAssembly = true, Namespace = \"Other\"")));
        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(oldTree, newTree));

        Assert.Equal(IncrementalStepRunReason.Modified, WalkReason(driver.GetRunResult().Results.Single()));
    }

    private static IncrementalStepRunReason WalkReason(GeneratorRunResult result) =>
        result.TrackedSteps[TrackingNames.WholeAssemblyWalk].Single().Outputs.Single().Reason;
}

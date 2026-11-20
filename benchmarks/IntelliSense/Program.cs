using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using PureMarkup.Maui.Benchmarks;
using PureMarkup.Maui.Generators;

// IntelliSense cost of PureMarkup in a consumer project. Usage: dotnet run -c Release [-- iterations]
// Each scenario gets a fresh AdhocWorkspace (fresh metadata references), so "cold" is the first
// call in that workspace after the process has JIT-warmed on a throwaway round.
var iterations = args.Length > 0 && int.TryParse(args[0], out var n) ? n : 30;

var scenarios = new[]
{
    new Scenario("Baseline (plain MAUI, no PureMarkup)", PureMarkup: false, Generator: false, WholeAssembly: false),
    new Scenario("PureMarkup (package reference)", PureMarkup: true, Generator: true, WholeAssembly: false),
    new Scenario("PureMarkup + consumer WholeAssembly walk", PureMarkup: true, Generator: true, WholeAssembly: true),
};

Console.Error.WriteLine("Warm-up round (JIT)...");
foreach (var s in scenarios)
{
    await Bench.CompletionAsync(s, 3);
    Bench.Generator(s, 3);
}

Console.WriteLine($"# IntelliSense benchmark ({iterations} warm iterations, {Environment.ProcessorCount} logical CPUs, {Environment.OSVersion}, .NET {Environment.Version})");
Console.WriteLine();
Console.WriteLine("## Completion (each iteration types one character in the page first)");
Console.WriteLine();
Console.WriteLine("| Scenario | Receiver | Items | Extension items | Cold ms | Warm median ms | Warm p90 ms | Extension items (first 4) |");
Console.WriteLine("|---|---|---:|---:|---:|---:|---:|---|");
var completion = new List<CompletionResult>();
foreach (var s in scenarios)
{
    foreach (var r in await Bench.CompletionAsync(s, iterations))
    {
        completion.Add(r);
        Console.WriteLine($"| {s.Name} | {r.Receiver} | {r.Items} | {r.ExtensionItems} | {r.Cold:F1} | {Stats.Median(r.Warm):F1} | {Stats.P90(r.Warm):F1} | {r.Sample} |");
    }
}

Console.WriteLine();
Console.WriteLine("## Per-keystroke compilation in the workspace (edit + GetCompilationAsync, includes generators)");
Console.WriteLine();
Console.WriteLine("| Scenario | Cold ms | Warm median ms | Warm p90 ms | Generated documents |");
Console.WriteLine("|---|---:|---:|---:|---:|");
foreach (var s in scenarios)
{
    var r = await Bench.WorkspaceCompilationAsync(s, iterations);
    Console.WriteLine($"| {s.Name} | {r.Cold:F1} | {Stats.Median(r.Warm):F1} | {Stats.P90(r.Warm):F1} | {r.GeneratedDocuments} |");
}

Console.WriteLine();
Console.WriteLine("## Generator driver, single-keystroke edit in the page");
Console.WriteLine();
Console.WriteLine("| Scenario | Cold run ms | Warm median ms | Warm p90 ms | Generated files | Step reasons on the edit run |");
Console.WriteLine("|---|---:|---:|---:|---:|---|");
foreach (var s in scenarios.Where(s => s.Generator))
{
    var r = Bench.Generator(s, iterations);
    Console.WriteLine($"| {s.Name} | {r.Cold:F1} | {Stats.Median(r.Warm):F1} | {Stats.P90(r.Warm):F1} | {r.GeneratedFiles} | {r.Steps} |");
}

Console.WriteLine();
Console.WriteLine("## Versus baseline (warm medians; same receiver in the baseline, or `new Button().` for PresetContext)");
Console.WriteLine();
foreach (var c in completion.Where(c => c.Scenario != scenarios[0]))
{
    var reference = completion.FirstOrDefault(b => b.Scenario == scenarios[0] && b.Receiver == c.Receiver)
        ?? completion.First(b => b.Scenario == scenarios[0] && b.Receiver == "new Button().");
    var baseline = Stats.Median(reference.Warm);
    var median = Stats.Median(c.Warm);
    Console.WriteLine($"- {c.Scenario.Name}, {c.Receiver}: {median / baseline:F2}x ({median - baseline:+0.0;-0.0} ms)");
}

internal sealed record Scenario(string Name, bool PureMarkup, bool Generator, bool WholeAssembly)
{
    public override string ToString() => Name;
}

internal sealed record CompletionResult(Scenario Scenario, string Receiver, int Items, int ExtensionItems, double Cold, List<double> Warm, string Sample);

internal sealed record TimingResult(double Cold, List<double> Warm, int GeneratedDocuments);

internal sealed record GeneratorResult(double Cold, List<double> Warm, int GeneratedFiles, string Steps);

internal static class Bench
{
    private static readonly (string Probe, string Marker)[] Probes =
    [
        ("new Button().", ConsumerSources.ButtonCaret),
        ("PresetContext<Button>", ConsumerSources.PresetCaret),
        ("\"text\". (unrelated)", ConsumerSources.StringCaret),
    ];

    public static async Task<List<CompletionResult>> CompletionAsync(Scenario scenario, int iterations)
    {
        var results = new List<CompletionResult>();
        foreach (var (probe, marker) in Probes)
        {
            if (marker == ConsumerSources.PresetCaret && !scenario.PureMarkup)
            {
                continue;
            }

            // Fresh workspace per receiver, so each cold number is a real first call.
            using var workspace = new AdhocWorkspace(MefHostServices.DefaultHost);
            var documentId = CreateProject(workspace, scenario);
            var timings = new List<double>();
            int items = 0, extensionItems = 0;
            var sample = "";
            for (var i = 0; i <= iterations; i++)
            {
                var (text, position) = PageText(scenario, i, marker);
                var solution = workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(text));
                workspace.TryApplyChanges(solution);
                var document = workspace.CurrentSolution.GetDocument(documentId)!;
                var service = CompletionService.GetService(document)!;

                var sw = Stopwatch.StartNew();
                var list = await service.GetCompletionsAsync(document, position, CompletionTrigger.CreateInsertionTrigger('.'));
                timings.Add(sw.Elapsed.TotalMilliseconds);

                items = list.ItemsList.Count;
                var extensions = list.ItemsList.Where(item => item.Tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.ExtensionMethod)).ToList();
                extensionItems = extensions.Count;
                sample = string.Join(", ", extensions.Select(item => item.DisplayText).Take(4)) + (extensions.Count > 4 ? ", ..." : "");
            }

            results.Add(new CompletionResult(scenario, probe, items, extensionItems, timings[0], timings.Skip(1).ToList(), sample));
        }

        return results;
    }

    public static async Task<TimingResult> WorkspaceCompilationAsync(Scenario scenario, int iterations)
    {
        using var workspace = new AdhocWorkspace(MefHostServices.DefaultHost);
        var documentId = CreateProject(workspace, scenario);
        var timings = new List<double>();
        var generated = 0;
        for (var i = 0; i <= iterations; i++)
        {
            var (text, _) = PageText(scenario, i, ConsumerSources.ButtonCaret);
            workspace.TryApplyChanges(workspace.CurrentSolution.WithDocumentText(documentId, SourceText.From(text)));
            var project = workspace.CurrentSolution.GetDocument(documentId)!.Project;

            var sw = Stopwatch.StartNew();
            _ = await project.GetCompilationAsync();
            timings.Add(sw.Elapsed.TotalMilliseconds);
            generated = (await project.GetSourceGeneratedDocumentsAsync()).Count();
        }

        return new TimingResult(timings[0], timings.Skip(1).ToList(), generated);
    }

    public static GeneratorResult Generator(Scenario scenario, int iterations)
    {
        var references = References(scenario);
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var otherTrees = OtherSources(scenario).Select(s => CSharpSyntaxTree.ParseText(s, parse)).ToList();
        var pageTree = CSharpSyntaxTree.ParseText(PageText(scenario, 0, ConsumerSources.ButtonCaret).Text, parse);
        var compilation = CSharpCompilation.Create("Consumer", [.. otherTrees, pageTree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new MarkupGenerator().AsSourceGenerator()],
            parseOptions: parse,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        var sw = Stopwatch.StartNew();
        driver = driver.RunGenerators(compilation);
        var cold = sw.Elapsed.TotalMilliseconds;

        var timings = new List<double>();
        GeneratorRunResult last = default;
        for (var i = 1; i <= iterations; i++)
        {
            // One keystroke: the page text with one more character typed, as a tree edit (what the IDE does).
            var newTree = pageTree.WithChangedText(SourceText.From(PageText(scenario, i, ConsumerSources.ButtonCaret).Text));
            compilation = compilation.ReplaceSyntaxTree(pageTree, newTree);
            pageTree = newTree;

            sw.Restart();
            driver = driver.RunGenerators(compilation);
            timings.Add(sw.Elapsed.TotalMilliseconds);
            last = driver.GetRunResult().Results.Single();
        }

        var steps = last.TrackedSteps
            .Select(kv => kv.Key + ": " + string.Join(", ", kv.Value
                .SelectMany(step => step.Outputs)
                .GroupBy(o => o.Reason)
                .Select(g => $"{g.Count()} {g.Key}")))
            .ToList();
        return new GeneratorResult(cold, timings, last.GeneratedSources.Length, string.Join("; ", steps));
    }

    private static DocumentId CreateProject(AdhocWorkspace workspace, Scenario scenario)
    {
        var projectId = ProjectId.CreateNewId();
        var info = ProjectInfo.Create(projectId, VersionStamp.Create(), "Consumer", "Consumer", LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable),
            parseOptions: new CSharpParseOptions(LanguageVersion.Latest),
            metadataReferences: References(scenario),
            analyzerReferences: scenario.Generator ? [new GeneratorReference()] : []);
        var solution = workspace.CurrentSolution.AddProject(info);
        var index = 0;
        foreach (var source in OtherSources(scenario))
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), $"File{index++}.cs", SourceText.From(source));
        }

        var pageId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(pageId, "MainPage.cs", SourceText.From(PageText(scenario, 0, ConsumerSources.ButtonCaret).Text));
        workspace.TryApplyChanges(solution);
        return pageId;
    }

    private static IEnumerable<string> OtherSources(Scenario scenario)
    {
        yield return ConsumerSources.ViewModel;
        yield return ConsumerSources.RatingView;
        if (scenario.Generator)
        {
            var attributes = "[assembly: PureMarkup.Maui.GenerateMarkup(typeof(Consumer.RatingView), Namespace = \"Consumer\")]\n";
            if (scenario.WholeAssembly)
            {
                // A sizable referenced assembly, walked at consumer time: all of Microsoft.Maui.Controls
                // again, into a namespace of its own so it does not clash with the package's methods.
                attributes += "[assembly: PureMarkup.Maui.GenerateMarkup(typeof(Microsoft.Maui.Controls.View), WholeAssembly = true, Namespace = \"Consumer.Markup\")]\n";
            }

            yield return attributes;
        }
    }

    /// <summary>The page after <paramref name="keystrokes"/> typed characters, with the caret at <paramref name="marker"/>.</summary>
    private static (string Text, int Position) PageText(Scenario scenario, int keystrokes, string marker)
    {
        var template = !scenario.PureMarkup
            ? ConsumerSources.PlainPage
            : ConsumerSources.MarkupPage.Replace("{USINGS}", scenario.WholeAssembly ? "using Consumer.Markup;" : "using PureMarkup.Maui;");
        template = template.Replace(ConsumerSources.Typed, new string('a', keystrokes));
        foreach (var other in new[] { ConsumerSources.ButtonCaret, ConsumerSources.PresetCaret, ConsumerSources.StringCaret })
        {
            if (other != marker)
            {
                template = template.Replace(other, "");
            }
        }

        var position = template.IndexOf(marker, StringComparison.Ordinal);
        return (template.Replace(marker, ""), position);
    }

    private static readonly Dictionary<bool, string[]> ReferencePaths = new()
    {
        [false] = ReferenceFiles(pureMarkup: false),
        [true] = ReferenceFiles(pureMarkup: true),
    };

    /// <summary>Fresh references every call, so every workspace pays its own metadata load.</summary>
    private static List<MetadataReference> References(Scenario scenario) =>
        ReferencePaths[scenario.PureMarkup].Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();

    /// <summary>What a MAUI app compiles against: the runtime, Microsoft.Maui.*, Microsoft.Extensions.*; not Roslyn or this tool.</summary>
    private static string[] ReferenceFiles(bool pureMarkup) =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p =>
            {
                var name = Path.GetFileName(p);
                return name == "PureMarkup.Maui.dll" ? pureMarkup
                    : name is "mscorlib.dll" or "netstandard.dll"
                    || name.StartsWith("Microsoft.Maui", StringComparison.Ordinal)
                    || name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
                    || name.StartsWith("Microsoft.Win32.", StringComparison.Ordinal)
                    || (name.StartsWith("System.", StringComparison.Ordinal) && !name.StartsWith("System.Composition", StringComparison.Ordinal));
            })
            .ToArray();
}

/// <summary>Hands the generator to the workspace as if it came from the package's analyzers folder.</summary>
internal sealed class GeneratorReference : AnalyzerReference
{
    private static readonly ISourceGenerator Instance = new MarkupGenerator().AsSourceGenerator();

    public override string? FullPath => typeof(MarkupGenerator).Assembly.Location;

    public override string Display => "PureMarkup.Maui.Generators";

    public override object Id => typeof(MarkupGenerator).Assembly.Location;

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => [];

    public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => [];

    public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => [Instance];

    public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages() => [Instance];
}

internal static class Stats
{
    public static double Median(List<double> values) => Percentile(values, 0.5);

    public static double P90(List<double> values) => Percentile(values, 0.9);

    private static double Percentile(List<double> values, double p)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(p * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}

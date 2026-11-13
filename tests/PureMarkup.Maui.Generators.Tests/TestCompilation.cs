using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PureMarkup.Maui.Generators.Tests;

internal static class TestCompilation
{
    /// <summary>Runtime assemblies only (no MAUI): for the fake-MAUI snapshot tests.</summary>
    public static IReadOnlyList<MetadataReference> Runtime { get; } = TrustedPlatformAssemblies()
        .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
        .ToList();

    /// <summary>Every assembly the test host loads (runtime, Microsoft.Maui.*, Microsoft.Extensions.*) except PureMarkup itself.</summary>
    public static IReadOnlyList<MetadataReference> WithMaui { get; } = AllTrustedPlatformAssemblies()
        .Where(p => Path.GetFileName(p) is not ("PureMarkup.Maui.dll" or "PureMarkup.Maui.Generators.dll" or "PureMarkup.Maui.Generators.Tests.dll"))
        .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
        .ToList();

    public static CSharpCompilation Create(string source, IEnumerable<MetadataReference> references) =>
        CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    public static (GeneratorDriver Driver, Compilation Output, IReadOnlyList<Diagnostic> GeneratorDiagnostics) Run(Compilation compilation)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new MarkupGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        return (driver, output, diagnostics);
    }

    private static IEnumerable<string> AllTrustedPlatformAssemblies() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);

    private static IEnumerable<string> TrustedPlatformAssemblies() =>
        AllTrustedPlatformAssemblies()
            .Where(p => Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal)
                || Path.GetFileName(p) is "mscorlib.dll" or "netstandard.dll");
}

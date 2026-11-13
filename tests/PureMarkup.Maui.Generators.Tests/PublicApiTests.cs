using PublicApiGenerator;

namespace PureMarkup.Maui.Generators.Tests;

/// <summary>
/// Snapshot of the public API of PureMarkup.Maui (net10.0), so any change to the generated
/// surface shows up in review as a diff of PublicApi.verified.txt.
/// </summary>
public class PublicApiTests
{
    [Fact]
    public Task PublicApiIsUnchanged()
    {
        var api = typeof(ButtonMarkup).Assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            ExcludeAttributes = ["System.Runtime.Versioning.TargetFrameworkAttribute", "System.Reflection.AssemblyMetadataAttribute"],
        });
        return Verify(api).UseFileName("PublicApi");
    }
}

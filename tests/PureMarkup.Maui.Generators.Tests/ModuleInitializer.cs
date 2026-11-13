using System.Runtime.CompilerServices;

namespace PureMarkup.Maui.Generators.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init() => VerifySourceGenerators.Initialize();
}

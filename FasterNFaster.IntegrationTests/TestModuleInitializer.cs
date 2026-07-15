using System.Runtime.CompilerServices;

namespace FasterNFaster.IntegrationTests;

internal static class TestModuleInitializer
{
    // The VSTest testhost process runs with reflection-based System.Text.Json disabled,
    // which leaves JsonSerializerOptions.TypeInfoResolver null and makes FastEndpoints'
    // response serialization throw. Re-enable it before any serialization occurs.
    [ModuleInitializer]
    internal static void Init() =>
        AppContext.SetSwitch("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true);
}

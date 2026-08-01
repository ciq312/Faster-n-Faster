using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FasterNFaster.Api.Infrastructure.Caching;

public static class CacheSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { HydrateNonPublicMembers } }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    // Lets cached domain entities round-trip without JSON attributes or invoking their validating constructors.
    private static void HydrateNonPublicMembers(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;

        if (typeInfo.Type.GetConstructor(Type.EmptyTypes) is null)
            typeInfo.CreateObject = () => RuntimeHelpers.GetUninitializedObject(typeInfo.Type);

        foreach (var property in typeInfo.Properties)
        {
            if (property.Set is not null) continue;
            if (property.AttributeProvider is PropertyInfo { SetMethod: { } setter })
                property.Set = (target, value) => setter.Invoke(target, [value]);
        }
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonatroxPortfolio.Shared.Contracts;

public static class ContentJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };
}

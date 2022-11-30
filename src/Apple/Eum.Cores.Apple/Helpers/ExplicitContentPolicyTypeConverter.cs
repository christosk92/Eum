using System.Text.Json;
using System.Text.Json.Serialization;
using Eum.Cores.Apple.Contracts.Models.Response.StoreFront;
using Eum.Cores.Apple.Models;

namespace Eum.Cores.Apple.Helpers;
internal sealed class ExplicitContentPolicyTypeConverter : JsonConverter<ExplicitContentPolicyType>
{
    public override ExplicitContentPolicyType Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var asString = reader.GetString();
        return asString switch
        {
            "allowed" => ExplicitContentPolicyType.Allowed,
            "opt-in" => ExplicitContentPolicyType.OptIn,
            "prohibited" => ExplicitContentPolicyType.Prohibited
        };
    }

    public override void Write(Utf8JsonWriter writer, ExplicitContentPolicyType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

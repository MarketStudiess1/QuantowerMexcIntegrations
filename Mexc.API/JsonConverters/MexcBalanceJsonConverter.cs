
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mexc.API.JsonConverters;

internal class MexcBalanceJsonConverter : JsonConverter<MexcBalance>
{
    public override MexcBalance ReadJson(JsonReader reader, Type objectType, MexcBalance existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcBalance
        {
            Asset = jObject["coin"].Value<string>(),
            Free = jObject["free"].Value<decimal>(),
            Locked = jObject["locked"].Value<decimal>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcBalance value, JsonSerializer serializer) { }
}
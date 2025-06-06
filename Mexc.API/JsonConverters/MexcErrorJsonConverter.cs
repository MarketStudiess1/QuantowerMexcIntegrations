using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mexc.API.JsonConverters;

internal class MexcErrorJsonConverter : JsonConverter<MexcError>
{
    public override MexcError ReadJson(JsonReader reader, Type objectType, MexcError existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcError
        {
            Code = jObject["code"].Value<int>(),
            Message = jObject["msg"].Value<string>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcError value, JsonSerializer serializer)
    {
    }
}
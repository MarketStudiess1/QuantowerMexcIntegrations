
using System;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcCandleJsonConverter : JsonConverter<MexcCandle>
{
    public override MexcCandle ReadJson(JsonReader reader, Type objectType, MexcCandle existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jArray = JArray.Load(reader);
        return new MexcCandle
        {
            Timestamp = jArray[0].Value<long>(),
            Open = jArray[1].Value<decimal>(),
            Close = jArray[4].Value<decimal>(),
            High = jArray[2].Value<decimal>(),
            Low = jArray[3].Value<decimal>(),
            Volume = jArray[5].Value<decimal>()
        };
    }

    public override void WriteJson(JsonWriter writer, MexcCandle value, JsonSerializer serializer) { }
}
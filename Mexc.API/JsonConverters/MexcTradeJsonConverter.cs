
using System;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcTradeJsonConverter : JsonConverter<MexcTrade>
{
    public override MexcTrade ReadJson(JsonReader reader, Type objectType, MexcTrade existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        return new MexcTrade
        {
            Pair = jObject["symbol"].ToString(),
            Id = jObject["id"].Value<long>(),
            Timestamp = jObject["time"].Value<long>(),
            Price = jObject["price"].Value<decimal>(),
            Amount = jObject["qty"].Value<decimal>()
        };
    }

    public override void WriteJson(JsonWriter writer, MexcTrade value, JsonSerializer serializer) { }
}
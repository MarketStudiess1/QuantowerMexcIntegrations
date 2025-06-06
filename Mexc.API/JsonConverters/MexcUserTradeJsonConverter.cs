
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mexc.API.JsonConverters;

internal class MexcUserTradeJsonConverter : JsonConverter<MexcUserTrade>
{
    public override MexcUserTrade ReadJson(JsonReader reader, Type objectType, MexcUserTrade existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcUserTrade
        {
            Symbol = jObject["symbol"].Value<string>(),
            Id = jObject["id"].Value<long>(),
            OrderId = jObject["orderId"].Value<string>(),
            Price = jObject["price"].Value<decimal>(),
            Quantity = jObject["qty"].Value<decimal>(),
            Fee = jObject["commission"].Value<decimal>(),
            FeeAsset = jObject["commissionAsset"].Value<string>(),
            Time = DateTimeOffset.FromUnixTimeMilliseconds(jObject["time"].Value<long>()).UtcDateTime,
            IsBuyer = jObject["isBuyer"].Value<bool>(),
            IsMaker = jObject["isMaker"].Value<bool>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcUserTrade value, JsonSerializer serializer) { }
}
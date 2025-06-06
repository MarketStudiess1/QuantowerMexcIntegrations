
using System;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcUserTradeJsonConverter : JsonConverter<MexcUserTrade>
{
    public override MexcUserTrade ReadJson(JsonReader reader, Type objectType, MexcUserTrade existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcUserTrade
        {
            Pair = jObject["symbol"].Value<string>(),
            Id = jObject["id"].Value<long>(),
            OrderId = jObject["orderId"].Value<string>(),
            Price = jObject["price"].Value<decimal>(),
            Amount = jObject["qty"].Value<decimal>(),
            Fee = jObject["commission"].Value<decimal>(),
            FeeCurrency = jObject["commissionAsset"].Value<string>(),
            ExecutionTime = DateTimeOffset.FromUnixTimeMilliseconds(jObject["time"].Value<long>()),
            IsBuyer = jObject["isBuyer"].Value<bool>(),
            IsMaker = jObject["isMaker"].Value<bool>()
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcUserTrade value, JsonSerializer serializer) { }
}
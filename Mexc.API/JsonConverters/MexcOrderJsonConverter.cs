using System;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcOrderJsonConverter : JsonConverter<MexcOrder>
{
    public override MexcOrder ReadJson(JsonReader reader, Type objectType, MexcOrder existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        return new MexcOrder
        {
            Symbol = jObject["symbol"].ToString(),
            OrderId = jObject["orderId"].ToString(),
            ClientOrderId = jObject["clientOrderId"]?.ToString(),
            Price = jObject["price"].Value<decimal>(),
            Quantity = jObject["origQty"].Value<decimal>(),
            ExecutedQty = jObject["executedQty"].Value<decimal>(),
            Status = jObject["status"].ToString(),
            Type = jObject["type"].ToString(),
            Side = jObject["side"].ToString(),
            Time = DateTimeOffset.FromUnixTimeMilliseconds(jObject["transactTime"].Value<long>())
        };
    }

    public override void WriteJson(JsonWriter writer, MexcOrder value, JsonSerializer serializer) { }
}

using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mexc.API.JsonConverters;

internal class MexcOrderResponseJsonConverter : JsonConverter<MexcOrderResponse>
{
    public override MexcOrderResponse ReadJson(JsonReader reader, Type objectType, MexcOrderResponse existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcOrderResponse
        {
            Symbol = jObject["symbol"].Value<string>(),
            OrderId = jObject["orderId"].Value<string>(),
            ClientOrderId = jObject["clientOrderId"]?.Value<string>(),
            TransactTime = DateTimeOffset.FromUnixTimeMilliseconds(jObject["transactTime"].Value<long>()).UtcDateTime
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcOrderResponse value, JsonSerializer serializer) { }
}
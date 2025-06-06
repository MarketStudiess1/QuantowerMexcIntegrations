using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcOrderBookJsonConverter : JsonConverter<MexcOrderBook>
{
    public override MexcOrderBook ReadJson(JsonReader reader, Type objectType, MexcOrderBook existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var bids = jObject["bids"].Select(b => new MexcOrderBookEntry
        {
            Price = b[0].Value<decimal>(),
            Quantity = b[1].Value<decimal>()
        }).ToArray();

        var asks = jObject["asks"].Select(a => new MexcOrderBookEntry
        {
            Price = a[0].Value<decimal>(),
            Quantity = a[1].Value<decimal>()
        }).ToArray();

        var result = new MexcOrderBook
        {
            LastUpdateId = jObject["lastUpdateId"].Value<long>(),
            Bids = bids,
            Asks = asks
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcOrderBook value, JsonSerializer serializer) { }
}
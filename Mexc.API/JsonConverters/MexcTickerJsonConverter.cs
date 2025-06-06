
using System;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API.JsonConverters;

internal class MexcTickerJsonConverter : JsonConverter<MexcTicker>
{
    public override MexcTicker ReadJson(JsonReader reader, Type objectType, MexcTicker existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        return new MexcTicker
        {
            Pair = jObject["symbol"].ToString(),
            LastPrice = jObject["lastPrice"].Value<decimal>(),
            High = jObject["highPrice"].Value<decimal>(),
            Low = jObject["lowPrice"].Value<decimal>(),
            Volume = jObject["volume"].Value<decimal>(),
            Bid = jObject["bidPrice"].Value<decimal>(),
            BidSize = jObject["bidQty"].Value<decimal>(),
            Ask = jObject["askPrice"].Value<decimal>(),
            AskSize = jObject["askQty"].Value<decimal>()
        };
    }

    public override void WriteJson(JsonWriter writer, MexcTicker value, JsonSerializer serializer) { }
}
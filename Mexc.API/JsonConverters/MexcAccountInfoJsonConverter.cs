
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Mexc.API.JsonConverters;

internal class MexcAccountInfoJsonConverter : JsonConverter<MexcAccountInfo>
{
    public override MexcAccountInfo ReadJson(JsonReader reader, Type objectType, MexcAccountInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jObject = serializer.Deserialize<JObject>(reader);

        var result = new MexcAccountInfo
        {
            MakerCommission = jObject["makerCommission"].Value<decimal>(),
            TakerCommission = jObject["takerCommission"].Value<decimal>(),
            CanTrade = jObject["canTrade"].Value<bool>(),
            CanWithdraw = jObject["canWithdraw"].Value<bool>(),
            CanDeposit = jObject["canDeposit"].Value<bool>(),
            UpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(jObject["updateTime"].Value<long>()).UtcDateTime
        };

        return result;
    }

    public override void WriteJson(JsonWriter writer, MexcAccountInfo value, JsonSerializer serializer) { }
}
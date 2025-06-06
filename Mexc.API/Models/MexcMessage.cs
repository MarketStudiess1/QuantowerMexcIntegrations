
using System.Runtime.Serialization;

namespace Mexc.API.Models;

[DataContract]
internal class MexcMessage
{
    [DataMember(Name = "c")]
    public string Channel { get; set; } // Channel name, e.g., "spot@public.ticker.v3.api@BTCUSDT"

    [DataMember(Name = "d")]
    public object Data { get; set; } // Data payload

    [DataMember(Name = "t")]
    public long? Timestamp { get; set; } // Timestamp in milliseconds
}
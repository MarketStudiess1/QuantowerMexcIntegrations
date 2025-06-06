using System.Runtime.Serialization;

namespace Mexc.API.Models;

[DataContract]
internal class MexcAuthMessage
{
    [DataMember(Name = "method")]
    public string Method { get; set; } = "SUBSCRIBE_AUTH";

    [DataMember]
    public string Parameters { get; set; } // Parameters, e.g., "listenKey"

    [DataMember(Name = "timestamp")]
    public long Timestamp { get; set; } // Timestamp in milliseconds

    [DataMember(Name = "signature")]
    public string Signature { get; set; } // HMAC SHA256 signature
}
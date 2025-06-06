
using System.Runtime.Serialization;

namespace Mexc.API.Models.Requests;

[DataContract]
public class MexcQueryOrderRequest
{
    [DataMember(Name = "symbol")]
    public string Symbol { get; set; } // Trading pair

    [DataMember(Name = "orderId")]
    public string OrderId { get; set; } // Order ID to query

    [DataMember(Name = "origClientOrderId")]
    public string ClientOrderId { get; set; } // Client-specified order ID
}
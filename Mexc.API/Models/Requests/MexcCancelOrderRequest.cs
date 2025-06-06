
using System.Runtime.Serialization;

namespace Mexc.API.Models.Requests;

[DataContract]
public class MexcCancelOrderRequest
{
    [DataMember(Name = "symbol")]
    public string Symbol { get; set; } // Trading pair

    [DataMember(Name = "orderId")]
    public string OrderId { get; set; } // Order ID to cancel

    [DataMember(Name = "newClientOrderId")]
    public string ClientOrderId { get; set; } // Client-specified order ID
}
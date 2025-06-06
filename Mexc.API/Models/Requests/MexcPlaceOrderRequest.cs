using System.Runtime.Serialization;

namespace Mexc.API.Models.Requests;

[DataContract]
public class MexcPlaceOrderRequest
{
    [DataMember(Name = "symbol")]
    public string Symbol { get; set; } // Trading pair, e.g., "BTCUSDT"

    [DataMember(Name = "side")]
    public string Side { get; set; } // Order side (BUY or SELL)

    [DataMember(Name = "type")]
    public string Type { get; set; } // Order type (LIMIT, MARKET, etc.)

    [DataMember(Name = "quantity")]
    public decimal Quantity { get; set; } // Order quantity

    [DataMember(Name = "price")]
    public decimal? Price { get; set; } // Price for limit orders

    [DataMember(Name = "timeInForce")]
    public string TimeInForce { get; set; } // Time in force (e.g., GTC, IOC)

    [DataMember(Name = "newClientOrderId")]
    public string ClientOrderId { get; set; } // Client-specified order ID
}
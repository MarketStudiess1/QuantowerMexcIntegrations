
using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents an order response from MEXC API.
/// </summary>
public class MexcOrderResponse
{
    public string Symbol { get; internal set; } // Trading pair
    public string OrderId { get; internal set; } // Order ID
    public string ClientOrderId { get; internal set; } // Client-specified order ID
    public DateTimeOffset TransactTime { get; internal set; } // Transaction time

    public override string ToString() => $"{this.Symbol} | {this.OrderId} | {this.TransactTime}";
}
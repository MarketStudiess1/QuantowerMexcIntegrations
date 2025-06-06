
using System;

namespace Mexc.API.Models;
/// <summary>
/// Represents an order from MEXC API.
/// </summary>
public class MexcTicker
{
    public string Symbol { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public string OrderId { get; internal set; } // Unique order identifier
    public string ClientOrderId { get; internal set; } // Client-specified order ID
    public decimal Price { get; internal set; } // Order price
    public decimal OrigQuantity { get; internal set; } // Original order quantity
    public decimal ExecutedQuantity { get; internal set; } // Executed quantity
    public string Status { get; internal set; } // Order status (e.g., "NEW", "FILLED")
    public string TimeInForce { get; internal set; } // Time in force (e.g., "GTC")
    public string Type { get; internal set; } // Order type (e.g., "LIMIT", "MARKET")
    public string Side { get; internal set; } // Order side (e.g., "BUY", "SELL")
    public DateTimeOffset Time { get; internal set; } // Order creation time
    public DateTimeOffset UpdateTime { get; internal set; } // Order update time

    public override string ToString() => $"{this.Symbol} | {this.OrderId} | {this.Type} | {this.Side} | {this.Status}";
}
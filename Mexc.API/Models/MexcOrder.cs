
using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents an order from MEXC API.
/// </summary>
public class MexcOrder
{
    public string Symbol { get; set; } // Trading pair, e.g., "BTCUSDT"
    public string OrderId { get; set; } // Unique order identifier
    public string ClientOrderId { get; set; } // Client-specified order ID
    public decimal Price { get; set; } // Order price
    public decimal OrigQuantity { get; set; } // Original order quantity
    public decimal ExecutedQuantity { get; set; } // Executed quantity
    public string Status { get; set; } // Order status (e.g., "NEW", "FILLED")
    public string TimeInForce { get; set; } // Time in force (e.g., "GTC")
    public string Type { get; set; } // Order type (e.g., "LIMIT", "MARKET")
    public string Side { get; set; } // Order side (e.g., "BUY", "SELL")
    public DateTimeOffset Time { get; set; } // Order creation time
    public DateTimeOffset UpdateTime { get; set; } // Order update time
}
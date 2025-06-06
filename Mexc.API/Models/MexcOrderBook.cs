
namespace Mexc.API.Models;

/// <summary>
/// Represents an order book snapshot from MEXC API.
/// </summary>
public class MexcOrderBook
{
    public long LastUpdateId { get; internal set; } // Last update ID
    public MexcOrderBookEntry[] Bids { get; internal set; } // Bid levels
    public MexcOrderBookEntry[] Asks { get; internal set; } // Ask levels

    public override string ToString() => $"UpdateId: {this.LastUpdateId} | Bids: {this.Bids.Length} | Asks: {this.Asks.Length}";
}

/// <summary>
/// Represents a single price level in the order book.
/// </summary>
public class MexcOrderBookEntry
{
    public decimal Price { get; internal set; } // Price level
    public decimal Quantity { get; internal set; } // Quantity at price level

    public override string ToString() => $"{this.Price} | {this.Quantity}";
}
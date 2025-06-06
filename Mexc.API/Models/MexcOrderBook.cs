using System.Collections.Generic;

namespace Mexc.API.Models;

/// <summary>
/// Represents an order book from MEXC API.
/// </summary>
public class MexcOrderBook
{
    public string Pair { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public long LastUpdateId { get; internal set; } // Last update ID
    public MexcOrderBookEntry[] Bids { get; internal set; } // Bid entries
    public MexcOrderBookEntry[] Asks { get; internal set; } // Ask entries

    public override string ToString() => $"{this.Pair} | Bids: {this.Bids.Length} | Asks: {this.Asks.Length}";
}

/// <summary>
/// Represents an order book entry.
/// </summary>
public class MexcOrderBookEntry
{
    public decimal Price { get; internal set; } // Price level
    public decimal Quantity { get; internal set; } // Quantity at price level
}
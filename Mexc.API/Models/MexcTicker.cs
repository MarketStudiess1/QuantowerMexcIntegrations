namespace Mexc.API.Models;

/// <summary>
/// Represents ticker data from MEXC API.
/// </summary>
public class MexcTicker
{
    public string Pair { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public decimal LastPrice { get; internal set; } // Last price
    public decimal High { get; internal set; } // 24h high price
    public decimal Low { get; internal set; } // 24h low price
    public decimal Volume { get; internal set; } // 24h volume
    public decimal Bid { get; internal set; } // Bid price
    public decimal BidSize { get; internal set; } // Bid quantity
    public decimal Ask { get; internal set; } // Ask price
    public decimal AskSize { get; internal set; } // Ask quantity

    public override string ToString() => $"{this.Pair} | Last: {this.LastPrice} | Bid: {this.Bid}/{this.BidSize} | Ask: {this.Ask}/{this.AskSize}";
}
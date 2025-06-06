namespace Mexc.API.Models;

/// <summary>
/// Represents a trade from MEXC API.
/// </summary>
public class MexcTrade
{
    public string Pair { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public long Id { get; internal set; } // Trade ID
    public long Time { get; internal set; } // Trade time in milliseconds
    public decimal Price { get; internal set; } // Trade price
    public decimal Amount { get; internal set; } // Trade quantity (qty)

    public override string ToString() => $"{this.Pair} | {this.Id} | {this.Price} | {this.Amount}";
}
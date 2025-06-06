using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents a user trade from MEXC API.
/// </summary>
public class MexcUserTrade
{
    public string Pair { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public long Id { get; internal set; } // Trade ID
    public string OrderId { get; internal set; } // Order ID
    public decimal Price { get; internal set; } // Trade price
    public decimal Amount { get; internal set; } // Trade quantity (qty)
    public decimal Fee { get; internal set; } // Commission fee
    public string FeeCurrency { get; internal set; } // Commission asset
    public DateTimeOffset ExecutionTime { get; internal set; } // Trade execution time
    public bool IsBuyer { get; internal set; } // True if buyer
    public bool IsMaker { get; internal set; } // True if maker

    public override string ToString() => $"{this.Pair} | {this.Id} | {this.Price} | {this.Amount}";
}
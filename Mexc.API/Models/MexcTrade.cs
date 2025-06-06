using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents a market trade from MEXC API.
/// </summary>
public class MexcTrade
{
    public long Id { get; internal set; } // Trade ID
    public decimal Price { get; internal set; } // Trade price
    public decimal Quantity { get; internal set; } // Trade quantity
    public DateTimeOffset Time { get; internal set; } // Trade time
    public bool IsBuyerMaker { get; internal set; } // True if buyer is the maker

    public override string ToString() => $"{this.Id} | {this.Time} | {this.Price} | {this.Quantity}";
}
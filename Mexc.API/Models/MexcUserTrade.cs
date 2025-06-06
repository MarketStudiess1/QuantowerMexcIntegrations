
using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents a user's trade from MEXC API.
/// </summary>
public class MexcUserTrade
{
    public string Symbol { get; internal set; } // Trading pair
    public long Id { get; internal set; } // Trade ID
    public string OrderId { get; internal set; } // Order ID
    public decimal Price { get; internal set; } // Trade price
    public decimal Quantity { get; internal set; } // Trade quantity
    public decimal Fee { get; internal set; } // Trade fee
    public string FeeAsset { get; internal set; } // Fee asset
    public DateTimeOffset Time { get; internal set; } // Trade time
    public bool IsBuyer { get; internal set; } // True if user is buyer
    public bool IsMaker { get; internal set; } // True if user is maker

    public override string ToString() => $"{this.Symbol} | {this.Id} | {this.Price} | {this.Quantity} | Fee: {this.Fee} {this.FeeAsset}";
}
using System;

namespace Mexc.API.Models;

/// <summary>
/// Represents account information from MEXC API.
/// </summary>
public class MexcAccountInfo
{
    public string AccountType { get; internal set; } // SPOT or FUTURES
    public decimal MakerCommission { get; internal set; } // Maker commission rate
    public decimal TakerCommission { get; internal set; } // Taker commission rate
    public bool CanTrade { get; internal set; }
    public bool CanWithdraw { get; internal set; }
    public bool CanDeposit { get; internal set; }
    public DateTimeOffset UpdateTime { get; internal set; } // Update time

    public override string ToString() => $"{this.AccountType} | CanTrade: {this.CanTrade}";
}
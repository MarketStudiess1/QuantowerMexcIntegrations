
using System;
using System.Collections.Generic;

namespace Mexc.API.Models;

/// <summary>
/// Event arguments for WebSocket data from MEXC API.
/// </summary>
public class MexcEventArgs : EventArgs
{
    public string Channel { get; internal set; } // Channel name

    public MexcTicker Ticker { get; internal set; } // Ticker update
    public MexcOrderBook OrderBook { get; internal set; } // Order book update
    public List<MexcOrderBookEntry> OrderBookSnapshot { get; internal set; } // Order book snapshot
    public MexcTrade Trade { get; internal set; } // Trade update
    public MexcOrder OrderUpdate { get; internal set; } // Order update
    public MexcUserTrade UserTrade { get; internal set; } // User trade update
    public MexcBalance BalanceUpdate { get; internal set; } // Balance update

    public bool IsSnapshotData { get; internal set; } // True if snapshot data

    public override string ToString() => $"Channel: {this.Channel} | Snapshot: {this.IsSnapshotData}";
}
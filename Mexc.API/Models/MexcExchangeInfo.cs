
namespace Mexc.API.Models;

/// <summary>
/// Represents exchange information from MEXC API.
/// </summary>
public class MexcExchangeInfo
{
    public string Timezone { get; internal set; } // Server timezone
    public long ServerTime { get; internal set; } // Server time in milliseconds
    public MexcSymbolInfo[] Symbols { get; internal set; } // List of trading pairs

    public override string ToString() => $"Timezone: {this.Timezone} | Symbols: {this.Symbols.Length}";
}

/// <summary>
/// Represents information about a trading pair.
/// </summary>
public class MexcSymbolInfo
{
    public string Symbol { get; internal set; } // Trading pair, e.g., "BTCUSDT"
    public string Status { get; internal set; } // Trading status
    public string BaseAsset { get; internal set; } // Base asset
    public string QuoteAsset { get; internal set; } // Quote asset
    public int QuantityPrecision { get; internal set; } // Quantity precision
    public int PricePrecision { get; internal set; } // Price precision

    public override string ToString() => $"{this.Symbol} | {this.BaseAsset}/{this.QuoteAsset} | Status: {this.Status}";
}
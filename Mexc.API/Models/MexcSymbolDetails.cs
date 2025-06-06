using System;

namespace Mexc.API.Models;

public class MexcSymbolDetails
{
    public string Symbol { get; set; }
    public string BaseAsset { get; set; }
    public string QuoteAsset { get; set; }
    public decimal QuantityPrecision { get; set; }
    public decimal PricePrecision { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal MinNotional { get; set; }
}
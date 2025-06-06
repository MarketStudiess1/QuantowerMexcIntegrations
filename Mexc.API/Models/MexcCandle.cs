
namespace Mexc.API.Models;
/// <summary>
/// Represents a candlestick (kline) from MEXC API.
/// </summary>
public class MexcCandle
{
    public long Timestamp { get; internal set; } // Open time in milliseconds
    public decimal Open { get; internal set; }
    public decimal Close { get; internal set; }
    public decimal High { get; internal set; }
    public decimal Low { get; internal set; }
    public decimal Volume { get; internal set; } // Base asset volume
    public decimal QuoteVolume { get; internal set; } // Quote asset volume

    public override string ToString() => $"{this.Timestamp} | O: {this.Open} | H: {this.High} | L: {this.Low} | C: {this.Close} | V: {this.Volume}";
}
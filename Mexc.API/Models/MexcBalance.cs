
namespace Mexc.API.Models;


/// <summary>
/// Represents an account balance from MEXC API.
/// </summary>
public class MexcBalance
{
    public string Asset { get; internal set; } // Asset name, e.g., "BTC"
    public decimal Free { get; internal set; } // Available balance
    public decimal Locked { get; internal set; } // Locked balance

    public override string ToString() => $"{this.Asset} | Free: {this.Free} | Locked: {this.Locked}";
}
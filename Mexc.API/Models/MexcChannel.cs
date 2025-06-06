
namespace Mexc.API.Models;

/// <summary>
/// WebSocket channel names for MEXC API.
/// </summary>
public static class MexcChannel
{
    public const string TICKER = "spot@public.ticker.v3.api";
    public const string DEPTH = "spot@public.depth.v3.api";
    public const string TRADES = "spot@public.deals.v3.api";
    public const string ORDER = "spot@private.order.v3.api";
    public const string ACCOUNT = "spot@private.account.v3.api";
}
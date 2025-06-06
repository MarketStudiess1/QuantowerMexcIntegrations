
namespace Mexc.API.Models;

/// <summary>
/// Order types supported by MEXC API.
/// </summary>
public static class MexcOrderType
{
    public const string LIMIT = "LIMIT";
    public const string MARKET = "MARKET";
    public const string LIMIT_MAKER = "LIMIT_MAKER";
    public const string STOP_LOSS = "STOP_LOSS";
    public const string STOP_LOSS_LIMIT = "STOP_LOSS_LIMIT";
    public const string TAKE_PROFIT = "TAKE_PROFIT";
    public const string TAKE_PROFIT_LIMIT = "TAKE_PROFIT_LIMIT";
}
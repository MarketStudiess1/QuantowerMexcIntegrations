
namespace Mexc.API.Models;

/// <summary>
/// Order statuses supported by MEXC API.
/// </summary>
public static class MexcOrderStatus
{
    public const string NEW = "NEW";
    public const string PARTIALLY_FILLED = "PARTIALLY_FILLED";
    public const string FILLED = "FILLED";
    public const string CANCELED = "CANCELED";
    public const string REJECTED = "REJECTED";
    public const string EXPIRED = "EXPIRED";
}
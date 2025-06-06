
using Mexc.API.Models;
using MexcVendor.Misc;

namespace MexcVendor.Extensions;

internal static class MexcBalanceExtensions
{
    public static MexcBalanceKey GetKey(this MexcBalance balance) => new(balance.Asset, balance.AccountType);
}
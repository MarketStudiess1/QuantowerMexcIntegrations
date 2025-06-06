
using TradingPlatform.BusinessLayer;

namespace MexcVendor.Extensions;

internal static class OrderTypeExtensions
{
    public static ValidateResult ValidateAccountType(this OrderType orderType, OrderRequestParameters parameters)
    {
        if (parameters.Account.Id.StartsWith(MexcAccountType.FUTURES))
        {
            if (parameters.Symbol.SymbolType != SymbolType.Swap)
                return ValidateResult.NotValid($"Futures account can only trade swap symbols. Selected: {parameters.Symbol.Name}");
        }
        else if (parameters.Account.Id.StartsWith(MexcAccountType.SPOT))
        {
            if (parameters.Symbol.SymbolType == SymbolType.Swap)
                return ValidateResult.NotValid($"Spot account cannot trade swap symbols. Selected: {parameters.Symbol.Name}");
        }

        return ValidateResult.Valid;
    }
}
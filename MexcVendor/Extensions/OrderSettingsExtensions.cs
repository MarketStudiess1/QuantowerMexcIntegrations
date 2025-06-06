
using Mexc.API.Models;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor.Extensions;

internal static class OrderSettingsExtensions
{
    public static IList<SettingItem> AddPostOnly(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type != RequestType.PlaceOrder)
            return settings;

        settings.Add(new SettingItemBoolean(OrderType.POST_ONLY, false, sortIndex)
        {
            Text = loc._("Post-only")
        });

        return settings;
    }

    public static bool IsPostOnly(this IList<SettingItem> settings) => settings.GetValueOrDefault(false, OrderType.POST_ONLY);

    public static IList<SettingItem> AddLeverage(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Symbol.SymbolType == SymbolType.Swap && parameters.Account.Id.StartsWith(MexcAccountType.FUTURES))
        {
            settings.Add(new SettingItemInteger(MexcVendor.LEVERAGE, 20, sortIndex)
            {
                Text = loc._("Leverage"),
                Minimum = 1,
                Maximum = MexcVendor.MAX_LEVERAGE,
                Increment = 1
            });
        }

        return settings;
    }

    public static int GetLeverage(this IList<SettingItem> settings) => settings.GetValueOrDefault(20, MexcVendor.LEVERAGE);

    public static IList<SettingItem> AddClientOrderId(this IList<SettingItem> settings, OrderRequestParameters parameters, int sortIndex)
    {
        if (parameters.Type == RequestType.ModifyOrder)
            return settings;

        long.TryParse(parameters.Comment, out long value);

        settings.Add(new SettingItemLong(MexcVendor.CLIENT_ORDER_ID, value, sortIndex)
        {
            Text = loc._("Comment"),
            Minimum = 0
        });

        return settings;
    }

    public static string GetClientOrderId(this IList<SettingItem> settings)
    {
        long? result = settings.GetValueOrDefault<long?>(null, MexcVendor.CLIENT_ORDER_ID);
        return result is null or < 1 ? null : result.ToString();
    }
}
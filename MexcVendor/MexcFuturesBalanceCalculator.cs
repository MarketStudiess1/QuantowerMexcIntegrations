using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;
using MexcVendor.Misc;

namespace TradingPlatform.Data;

public class MexcFuturesBalanceCalculator : LimitCalculator
{
    private readonly MexcContext mexcContext;
    private ItemSetting leverageItem;

    public MexcFuturesBalanceCalculator(MexcContext context)
    {
        this.mexcContext = context;
    }

    protected override void PopulateAction(ItemSetting[] orderSettings, OrderRequestParameters requestParameters)
    {
        this.leverageItem = orderSettings?.GetItemByName(MexcVendor.LEVERAGE);

        base.ExecuteOrder(orderSettings, requestParameters);
    }

    protected override void OnOrderSettingChanged(string settingName)
    {
        switch (settingName)
        {
            case MexcVendor.LEVERAGE:
                this.UpdateTotalLink();
                this.Calculate();
                break;

            default:
                base.OnOrderSetting(settingName);
                break;
        }
    }

    protected override double? GetAvailableForOrder()
    {
        if (this.CurrentSymbol == null)
            return null;

        var key = new Key(MexcAccountType.FUTURES, this.CurrentSymbol.Quoting.Symbol.Id);
        if (!this.mexcContext.Balances.TryGetValue(key, out var balance))
            return null;

        return balance.Free.ToDouble();
    }

    protected override int? Get(int leverage) => this.leverageItem?.GetValue<int>() ?? 1;
}
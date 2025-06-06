using Microsoft.Xna.Framework;
using System;

using MexcVendor.Misc;

using System.Collections.Generic;

using Microsoft.Extensions;
using System.Linq;

namespace TradingPlatform.Data;

public class MexcSpotBalanceCalculator : LimitCalculator
{
    private readonly MexcContext mexcContext;
    private ItemSetting sideItem;

    public MexcSpotBalanceCalculator(MexcContext context)
    {
        this.mexcContext = context;
    }

    protected override void PopulateAction(ItemSetting[] orderSettings, OrderRequestParameters requestParameters)
    {
        this.sideItem = orderSettings?.GetItemByName(OrderType.SIDE);

        base.ExecuteOrder(orderSettings, requestParameters);
    }

    protected override void OnSideChanged()
    {
        this.UpdateTotalLink();
        this.Calculate();

        base.OnSideChanged();
    }

    protected override double? GetAvailableForOrder()
    {
        if (this.CurrentSymbol == null)
            return null;

        var side = this.GetSide();
        string asset = side == Side.Buy ? this.CurrentSymbol.Quoting.Symbol.Id : this.CurrentSymbol.Base.Symbol.Id;
        var key = new Key(MexcAccountType.SPOT, asset);
        if (!this.mexcContext.Balances.TryGetValue(key, out var balance))
            return null;

        return balance.Free.ToDouble();
    }

    protected override int? Get(int leverage) => 1;

    private Side? GetSide() => (Side?)this.sideItem?.GetValue<int>();
}
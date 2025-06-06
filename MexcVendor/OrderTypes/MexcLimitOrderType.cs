
using MexcVendor.Extensions;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor.OrderTypes;

public class MexcLimitOrderType : LimitOrderType
{
    public MexcLimitOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    { }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings) =>
        base.GetOrderSettings(parameters, formatSettings)
            .AddPostOnly(parameters, 100)
            .AddLeverage(parameters, 110)
            .AddClientOrderId(parameters, 120);

    public override ValidateResult ValidateOrderRequestParameters(OrderRequestParameters parameters)
    {
        var result = base.ValidateOrderRequestParameters(parameters);
        if (result.State != ValidateState.Valid)
            return result;

        result = this.ValidateAccountType(parameters);
        if (result.State != ValidateState.Valid)
            return result;

        return ValidateResult.Valid;
    }
}
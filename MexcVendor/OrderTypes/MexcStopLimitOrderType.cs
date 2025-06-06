
using MexcVendor.Extensions;
using System.Collections.Generic;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor.OrderTypes;

public class MexcStopLimitOrderType : StopLimitOrderType
{
    public MexcStopLimitOrderType(params TimeInForce[] allowedTimeInForce)
        : base(allowedTimeInForce)
    { }

    public override IList<SettingItem> GetOrderSettings(OrderRequestParameters parameters, FormatSettings formatSettings) =>
        base.GetOrderSettings(parameters, formatSettings)
            .AddLeverage(parameters, 100)
            .AddClientOrderId(parameters, 110);

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
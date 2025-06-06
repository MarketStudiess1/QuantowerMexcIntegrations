// Copyright [Your Name] © 2025.

using System;
using System.Collections.Generic;
using System.Threading;
using Mexc.API.Abstractions;
using Mexc.API.Misc;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API;

internal class MexcPrivateWebSocketApi : MexcWebSocketApi, IMexcPrivateWebSocketApi
{
    private readonly string apiKey, apiSecret;
    private readonly MexcAuthHelper authHelper;

    public MexcPrivateWebSocketApi(string endpoint, string apiKey, string apiSecret, MexcAuthHelper authHelper)
        : base(endpoint)
    {
        this.apiKey = apiKey;
        this.apiSecret = apiSecret;
        this.authHelper = authHelper;
    }

    public string Authenticate()
    {
        long timestamp = authHelper.GenerateTimestamp();
        string signature = MexcAuthHelper.ComputeSignature($"accessKey={apiKey}&timestamp={timestamp}", apiSecret);

        var message = new
        {
            method = "SUBSCRIBE",
            args = new[] { $"private.account.order@private?accessKey={apiKey}&timestamp={timestamp}&signature={signature}" }
        };

        var response = SendMessage(JsonConvert.SerializeObject(message), CancellationToken.None);
        return response.ContainsKey("code") && response["code"] != "0" ? response["msg"]?.ToString() ?? "Authentication failed" : null;
    }

    protected override void DeserializeData(JObject message)
    {
        if (!message.ContainsKey("channel"))
            return;

        string channel = message["channel"].ToString();
        var data = message["data"];

        switch (channel)
        {
            case "private.account.order@private":
                var order = new MexcOrder
                {
                    Symbol = data["symbol"].ToString(),
                    OrderId = data["orderId"].ToString(),
                    ClientOrderId = data["clientOrderId"]?.ToString(),
                    Price = data["price"].Value<decimal>(),
                    Quantity = data["origQty"].Value<decimal>(),
                    ExecutedQty = data["executedQty"].Value<decimal>(),
                    Status = data["status"].ToString(),
                    Type = data["type"].ToString(),
                    Side = data["side"].ToString(),
                    Time = data["transactTime"].Value<long>()
                };
                OnNewData(new MexcEventArgs { OrderUpdate = order });
                break;

            case "private.account.trade@private":
                var trade = new MexcUserTrade
                {
                    Id = data["tradeId"].Value<long>(),
                    Pair = data["symbol"].ToString(),
                    OrderId = data["orderId"].Value<long>(),
                    Amount = data["qty"].Value<decimal>(),
                    Price = data["price"].Value<decimal>(),
                    Fee = data["commission"].Value<decimal>(),
                    FeeCurrency = data["commissionAsset"].ToString(),
                    ExecutionTime = data["transactTime"].Value<long>()
                };
                OnNewData(new MexcEventArgs { UserTrade = trade });
                break;

            case "private.account.balance@private":
                var balance = new MexcBalance
                {
                    Asset = data["asset"].ToString(),
                    Free = data["free"].Value<decimal>(),
                    Locked = data["locked"].Value<decimal>()
                };
                OnNewData(new MexcEventArgs { BalanceUpdate = balance });
                break;
        }
    }
}
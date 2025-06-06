// Copyright [Your Name] © 2025.

using System;
using System.Collections.Generic;
using System.Threading;
using Mexc.API.Abstractions;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mexc.API;

internal class MexcPublicWebSocketApi : MexcWebSocketApi, IMexcPublicWebSocketApi
{
    private readonly IDictionary<string, string> subscriptionCache;

    public MexcPublicWebSocketApi(string endpoint) : base(endpoint)
    {
        this.subscriptionCache = new Dictionary<string, string>();
    }

    public void Subscribe(string channel, string symbol, CancellationToken cancellation, out string error)
    {
        error = "";
        try
        {
            var message = new
            {
                method = "SUBSCRIBE",
                args = new[] { $"{channel}@{symbol.ToLower()}" }
            };

            var response = SendMessage(JsonConvert.SerializeObject(message), cancellation);
            if (response.ContainsKey("code") && response["code"] != "0")
            {
                error = response["msg"]?.ToString() ?? "Subscription failed";
                return;
            }

            subscriptionCache.TryAdd($"{channel}@{symbol}", symbol);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    public void Unsubscribe(string channel, string symbol, CancellationToken cancellation, out string error)
    {
        error = "";
        try
        {
            var message = new
            {
                method = "UNSUBSCRIBE",
                args = new[] { $"{channel}@{symbol.ToLower()}" }
            };

            var response = SendMessage(JsonConvert.SerializeObject(message), cancellation);
            if (response.ContainsKey("code") && response["code"] != "0")
            {
                error = response["msg"]?.ToString() ?? "Unsubscription failed";
                return;
            }

            subscriptionCache.Remove($"{channel}@{symbol}");
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    protected override void DeserializeData(JObject message)
    {
        if (!message.ContainsKey("channel"))
            return;

        string channel = message["channel"].ToString();
        var data = message["data"];

        switch (channel)
        {
            case "spot@public.ticker.v3.api":
                var ticker = new MexcTicker
                {
                    Pair = data["symbol"].ToString(),
                    LastPrice = data["lastPrice"].Value<decimal>(),
                    High = data["highPrice"].Value<decimal>(),
                    Low = data["lowPrice"].Value<decimal>(),
                    Volume = data["volume"].Value<decimal>(),
                    Bid = data["bidPrice"].Value<decimal>(),
                    BidSize = data["bidQty"].Value<decimal>(),
                    Ask = data["askPrice"].Value<decimal>(),
                    AskSize = data["askQty"].Value<decimal>()
                };
                OnNewData(new MexcEventArgs { Ticker = ticker });
                break;

            case "spot@public.deals.v3.api":
                var trade = new MexcTrade
                {
                    Pair = data["symbol"].ToString(),
                    Id = data["tradeId"].Value<long>(),
                    Timestamp = data["tradeTime"].Value<long>(),
                    Price = data["price"].Value<decimal>(),
                    Amount = data["qty"].Value<decimal>()
                };
                OnNewData(new MexcEventArgs { Trade = trade });
                break;

            case "spot@public.depth.v3.api":
                var book = new MexcOrderBook
                {
                    Pair = data["symbol"].ToString(),
                    Bids = data["bids"].Select(b => new MexcBookItem
                    {
                        Price = b[0].Value<decimal>(),
                        Amount = b[1].Value<decimal>()
                    }).ToList(),
                    Asks = data["asks"].Select(a => new MexcBookItem
                    {
                        Price = a[0].Value<decimal>(),
                        Amount = a[1].Value<decimal>()
                    }).ToList()
                };
                OnNewData(new MexcEventArgs { Book = book });
                break;
        }
    }
}
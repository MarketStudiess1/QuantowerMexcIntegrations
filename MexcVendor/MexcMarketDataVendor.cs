using Mexc.API.Models;
using MexcVendor.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor;

internal class MexcMarketDataVendor : MexcInternalVendor
{
    #region Properties

    private readonly AggressorFlagCalculator aggressorFlagCalculator;

    #endregion Properties

    public MexcMarketDataVendor()
    {
        this.aggressorFlagCalculator = new AggressorFlagCalculator();
    }

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        var baseResult = base.Connect(parameters);
        if (baseResult.State != ConnectionState.Connected)
            return baseResult;

        var cancellation = parameters.CancellationToken;

        // Symbols
        var exchangeInfo = this.HandleApiResponse(
            () => this.Api.PublicRestApi.GetExchangeInfo(cancellation), cancellation, out string error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        foreach (var symbol in exchangeInfo.Symbols)
        {
            this.Context.Symbols[symbol.Symbol] = symbol;
            if (symbol.Symbol.EndsWith("_PERP"))
                this.Context.Futures.Add(symbol.Symbol);
        }

        // Tickers
        var tickers = this.HandleApiResponse(
            () => this.Api.PublicRestApi.GetTickers(cancellation), cancellation, out string error);

        if (!string.IsNullOrEmpty(error))
            return ConnectionResult.CreateFail(error);

        if (cancellation.IsCancellationRequested)
            return ConnectionResult.CreateCancelled();

        this.Context.UpdateTickers(tickers);

        return ConnectionResult.CreateSuccess();
    }

    public override void OnConnected(CancellationToken token)
    {
        this.Api.PublicWebSocketApi.NewData += this.PublicWebSocketApiOnNewData;
        this.Api.PublicWebSocketApi.Error += this.WebSocketApiOnError;
        base.OnConnected(token);
    }

    public override void Disconnect()
    {
        if (this.Api.PublicWebSocketApi != null)
        {
            this.Api.PublicWebSocketApi.NewData -= this.PublicWebSocketApiOnNewData;
            this.Api.PublicWebSocketApi.Error -= this.WebSocketApiOnError;
        }
        this.Context.Symbols.Clear();
        this.aggressorFlagCalculator.Dispose();
        base.Disconnect();
    }

    #endregion Connection

    #region Symbols and symbol groups

    public override IList<MessageSymbol> GetSymbols(CancellationToken token)
    {
        var result = new List<MessageSymbol>();

        foreach (var item in this.Context.Symbols)
        {
            var mexcSymbol = item.Value;

            if (!MexcContext.TryParseAssets(mexcSymbol.Symbol, out string baseAsset, out string quoteAsset))
                continue;

            var messageSymbol = this.CreateMessageSymbol(mexcSymbol, baseAsset, quoteAsset);
            result.Add(messageSymbol);

            this.Context.LastTradeTimes.Add(mexcSymbol.Symbol, 0);
        }

        return result;
    }

    public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => new()
    {
        SymbolTypes = new List<SymbolType> { SymbolType.Crypto, SymbolType.Swap }
    };

    public override IList<MessageAsset> GetAssets(CancellationToken token)
    {
        var result = new List<MessageAsset>();

        foreach (var item in this.Context.Symbols)
        {
            var mexcSymbol = item.Value;

            if (!MexcContext.TryParseAssets(mexcSymbol.Symbol, out string baseAsset, out string quoteAsset))
            {
                Core.Instance.Loggers.Log($"{MexcVendor.VENDOR_NAME}. Can't create assets for symbol {mexcSymbol.Symbol}");
                continue;
            }

            result.Add(this.CreateMessageAsset(baseAsset));
            result.Add(this.CreateMessageAsset(quoteAsset));
        }

        return result;
    }

    public override IList<MessageExchange> GetExchanges(CancellationToken token)
    {
        return new List<MessageExchange>
        {
            new()
            {
                Id = MexcVendor.EXCHANGE_ID.ToString(),
                ExchangeName = "Exchange"
            }
        };
    }

    #endregion Symbols and symbol groups

    #region Rules

    public override IList<MessageRule> GetRules(CancellationToken token) => new List<MessageRule>
    {
        new() { Name = Rule.ALLOW_TRADING, Value = false }
    };

    #endregion Rules

    #region Subscriptions

    public override void SubscribeSymbol(SubscribeQuotesParameters parameters)
    {
        string channel = parameters.SubscribeType switch
        {
            SubscribeQuoteType.Quote => "spot@public.ticker",
            SubscribeQuoteType.Level2 => "spot@public.book",
            SubscribeQuoteType.Last => "spot@public.deals",
            _ => null
        };

        if (string.IsNullOrEmpty(channel))
            return;

        this.Api.PublicWebSocketApi.Subscribe(channel, parameters.SymbolId, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
    }

    public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters)
    {
        string channel = parameters.SubscribeType switch
        {
            SubscribeQuoteType.Quote => "spot@public.ticker",
            SubscribeQuoteType.Level2 => "spot@public.book",
            SubscribeQuoteType.Last => "spot@public.deals",
            _ => null
        };

        if (string.IsNullOrEmpty(channel))
            return;

        this.Api.PublicWebSocketApi.Unsubscribe(channel, parameters.SymbolId, parameters.CancellationToken, out string error);

        if (!string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
    }

    #endregion Subscriptions

    #region History

    public override HistoryMetadata GetHistoryMetadata(CancellationToken cancellation) => new()
    {
        AllowedAggregations = new string[] { HistoryAggregation.TICK, HistoryAggregation.TIME },
        AllowedPeriodsHistoryAggregationTime = new[]
        {
            Period.MIN1, Period.MIN5, Period.MIN15, Period.MIN30,
            Period.HOUR1, Period.HOUR4, Period.DAY1, Period.WEEK1, Period.MONTH1
        },
        AllowedHistoryTypesHistoryAggregationTime = new[] { HistoryType.Last },
        AllowedHistoryTypesHistoryAggregationTick = new[] { HistoryType.Last },
        DownloadingStep = TimeSpan.FromDays(1),
    };

    public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters parameters)
    {
        var result = new List<IHistoryItem>();
        string symbol = parameters.SymbolId;
        long fromUnix = new DateTimeOffset(parameters.FromTime).ToUnixTimeSeconds();
        long toUnix = new DateTimeOffset(parameters.ToTime).ToUnixTimeSeconds();
        var cancellation = parameters.TokenCancellationToken;

        var itemsStack = new Stack<List<IHistoryItem>>();

        switch (parameters.Aggregation)
        {
            case HistoryAggregationTick:
                {
                    long currentToUnix = toUnix;

                    while (fromUnix < currentToUnix)
                    {
                        var trades = this.HandleApiResponse(
                            () => this.Api.PublicRestApiV3.GetRecentTrades(symbol, fromUnix, currentToUnix, cancellation),
                            cancellation, out _, true, true);

                        if (trades == null || !trades.Any())
                            break;

                        var ticks = trades.Select(CreateHistoryItem).ToList();
                        itemsStack.Push(ticks);

                        currentToUnix = trades.Min(t => t.Time);
                    }
                }
                break;

            case HistoryAggregationTime when parameters.Aggregation is HistoryAggregationTime historyAggregationTime:
                {
                    string interval = historyAggregationTime.Period.ToString();
                    long currentToUnix = toUnix;

                    while (fromUnix < currentToUnix)
                    {
                        var candles = this.HandleApiResponse(
                            () => this.Api.PublicRestApiV3.GetKlines(symbol, interval, fromUnix, currentToUnix, cancellation),
                            cancellation, out _, true, true);

                        if (candles == null || !candles.Any())
                            break;

                        var bars = candles.Select(CreateHistoryItemBar).ToList();
                        itemsStack.Push(bars);

                        currentToUnix = candles.Min(c => c.OpenTime);
                    }
                }
                break;

            default:
                return result;
        }

        while (itemsStack.Any())
        {
            var items = itemsStack.Pop();
            for (int i = items.Count - 1; i >= 0; i--)
                result.Add(items[i]);
        }

        return result;
    }

    #endregion History

    #region Factory

    private MessageAsset CreateMessageAsset(string assetId) => new()
    {
        Id = assetId,
        Name = assetId,
        MinimumChange = string.Equals(assetId, MexcVendor.USER_ASSET_ID, StringComparison.Ordinal) ? 1e-2 : 1e-8
    };

    private MessageSymbol CreateMessageSymbol(MexcSymbolDetails symbolDetails, string baseAsset, string quoteAsset)
    {
        bool isPerpetual = this.Context.Futures.Contains(symbolDetails.Symbol);

        var message = new MessageSymbol(symbolDetails.Symbol)
        {
            AllowCalculateRealtimeChange = true,
            AllowCalculateRealtimeVolume = true,
            AllowCalculateRealtimeTrades = true,
            AllowCalculateRealtimeTicks = true,
            AllowAbbreviatePriceByTickSize = true,
            Description = isPerpetual ? $"{baseAsset}/Perpetual futures" : $"{baseAsset} vs {quoteAsset}",
            ExchangeId = MexcVendor.EXCHANGE_ID.ToString(),
            HistoryType = HistoryType.Last,
            LotSize = 1,
            LotStep = (double)symbolDetails.QuantityPrecision,
            MinLot = (double)symbolDetails.MinQuantity,
            MaxLot = double.MaxValue,
            NotionalValueStep = (double)symbolDetails.PricePrecision,
            ProductAssetId = baseAsset,
            QuotingCurrencyAssetId = quoteAsset,
            SymbolType = isPerpetual ? SymbolType.Swap : SymbolType.Crypto,
            VolumeType = SymbolVolumeType.Volume,
            QuotingType = SymbolQuotingType.LotSize,
            DeltaCalculationType = DeltaCalculationType.TickDirection,
            VariableTickList = new List<VariableTick>
            {
                new((double)Math.Pow(10, -symbolDetails.PricePrecision.ToDouble()))
            }
        };

        return message;
    }

    private DayBar CreateDayBar(MexcTicker ticker) => new(ticker.Pair, Core.InstanceTimeUtils.DateTimeUtcNow)
    {
        Change = (double)ticker.PriceChange,
        ChangePercentage = (double)ticker.PriceChangePercent,
        High = (double)ticker.High,
        Low = (double)ticker.Low,
        Volume = (double)ticker.Volume
    };

    private Quote CreateQuote(MexcTicker ticker)
    {
        var dateTime = Core.InstanceTimeUtils.DateTimeUtcNow;

        if (this.Context.LastTradeTimes.TryGetValue(ticker.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
            dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

        this.Context.LastTradeTimes[ticker.Pair] = dateTime.Ticks;

        return new Quote(ticker.Pair, (double)ticker.Bid, (double)ticker.BidSize, (double)ticker.Ask, (double)ticker.AskSize, dateTime);
    }

    private static DOMQuote CreateDomQuote(IReadOnlyCollection<MexcBookItem> bookItems)
    {
        var utcNow = Core.InstanceTimeUtils.DateTimeUtcNow;
        string symbol = bookItems.First().Symbol;

        var bids = bookItems.Where(b => b.Quantity > 0).OrderByDescending(b => b.Price);
        var asks = bookItems.Where(b => b.Quantity < 0).OrderBy(b => b.Price);

        var dom = new DOMQuote(symbol, utcNow);

        foreach (var bid in bids)
        {
            string id = $"Id_{bid.Price}";
            double price = (double)bid.Price;
            double size = (double)bid.Quantity;

            dom.Bids.Add(new Level2Quote(QuotePriceType.Bid, symbol, id, price, size, utcNow));
        }

        foreach (var ask in asks)
        {
            string id = $"Id_{ask.Price}";
            double price = (double)ask.Price;
            double size = (double)Math.Abs(ask.Quantity);

            dom.Asks.Add(new Level2Quote(QuotePriceType.Ask, symbol, id, price, size, utcNow));
        }

        return dom;
    }

    private Last CreateLast(MexcTrade trade)
    {
        var dateTime = DateTimeOffset.FromUnixTimeSeconds((int)trade.Time).UtcDateTime;

        if (this.Context.LastTradeTimes.TryGetValue(trade.Pair, out long lastTradeTime) && dateTime.Ticks <= lastTradeTime)
            dateTime = new DateTime(lastTradeTime + 1, DateTimeKind.Utc);

        this.Context.LastTradeTimes[trade.Pair] = dateTime.Ticks;

        return new Last(trade.Pair, (double)trade.Price, (double)trade.Amount, dateTime)
        {
            TradeId = trade.Id.ToString(),
            AggressorFlag = trade.IsBuyerMaker ? AggressorFlag.Sell : AggressorFlag.Buy
        };
    }

    private static IHistoryItem CreateHistoryItem(MexcTrade trade) => new HistoryItemLast
    {
        TicksLeft = DateTimeOffset.FromUnixTimeSeconds(trade.Time).UtcDateTime.Ticks,
        Price = (double)trade.Price,
        Volume = (double)trade.Amount
    };

    private static IHistoryItem CreateHistoryItemBar(MexcKline kline) => new HistoryItemBar
    {
        TicksLeft = DateTimeOffset.FromUnixTimeSeconds(kline.OpenTime).UtcDateTime.Ticks,
        Open = (double)kline.OpenPrice,
        High = (double)kline.HighPrice,
        Low = (double)kline.LowPrice,
        Close = (double)kline.ClosePrice,
        Volume = (double)kline.Volume
    };

    #endregion Factory

    #region Periodic actions

    private void UpdateTickersAction()
    {
        var tickers = this.HandleApiResponse(
            () => this.Api.PublicRestApi.GetTickers(this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateTickersAction)}: {error}", LoggingLevel.Error, MexcVendor.VENDOR_NAME);
            return;
        }

        this.Context.UpdateTickers(tickers);
    }

    #endregion Periodic actions

    private void PublicWebSocketApiOnNewData(object sender, MexcEventArgs e)
    {
        if (e.Ticker != null)
        {
            var dayBar = this.CreateDayBar(e.Ticker);
            this.PushMessage(dayBar);

            var quote = this.CreateQuote(e.Ticker);
            this.aggressorFlagCalculator.CollectBidAsk(quote);
            this.PushMessage(quote);
        }

        if (e.BookItems != null)
        {
            var dom = CreateDomQuote(e.BookItems);
            this.PushMessage(dom);
        }

        if (e.Trade != null)
        {
            var last = this.CreateLast(e.Trade);
            this.PushMessage(last);
        }
    }

    private void WebSocketApiOnError(object sender, MexcErrorEventArgs e)
    {
        if (e.Exception == null)
            return;

        this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(e.Exception.GetFullMessageRecursive()));
    }

    protected override void OnTimerTick()
    {
        base.OnTimerTick();
        Task.Run(this.UpdateTickersAction);
    }
}
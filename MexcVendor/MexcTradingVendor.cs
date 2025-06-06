using Mexc.API;
using Mexc.API.Models;
using MexcVendor.Extensions;
using MexcVendor.Misc;
using MexcVendor.OrderTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor;

internal class MexcTradingVendor : MexcMarketDataVendor
{
    #region Properties

    private static readonly string[] assetsForCrossRates = { "BTC", "USDT" };
    private readonly Dictionary<string, MessageCryptoAssetBalances> sentBalancesCache;

    private Task tickersUpdateTask;
    private Task balancesUpdateTask;

    #endregion Properties

    public MexcTradingVendor() => this.sentBalancesCache = new Dictionary<string, MessageCryptoAssetBalances>();

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        var baseResult = base.Connect(parameters);
        if (baseResult.State != ConnectionState.Connected)
            return baseResult;

        return ConnectionResult.CreateSuccess();
    }

    protected override MexcApi CreateApiClient(ConnectRequestParameters parameters)
    {
        var settings = parameters.ConnectionSettings;
        string apiKey = settings.GetValue<string>(LOGIN_PARAMETER_GROUP, MexcVendor.PARAMETER_API_KEY);
        string secretKey = settings.GetValue<string>(LOGIN_PARAMETER_GROUP, MexcVendor.PARAMETER_SECRET_KEY);
        return new MexcApi(apiKey, secretKey, parameters.CancellationToken);
    }

    public override void OnConnected(CancellationToken token)
    {
        this.Api.PrivateWebSocketApi.NewData += this.PrivateWebSocketApiOnNewData;
        this.Api.PrivateWebSocketApi.Error += this.WebSocketApiOnError;
        base.OnConnected(token);
    }

    public override void Disconnect()
    {
        if (this.Api?.PrivateWebSocketApi != null)
        {
            this.Api.PrivateWebSocketApi.NewData -= this.PrivateWebSocketApiOnNewData;
            this.Api.PrivateWebSocketApi.Error -= this.WebSocketApiOnError;
        }
        this.sentBalancesCache.Clear();
        base.Disconnect();
    }

    #endregion Connection

    #region Accounts and rules

    public override IList<MessageAccount> GetAccounts(CancellationToken token)
    {
        var accountInfo = this.HandleApiResponse(
            () => this.Api.PrivateRestApi.GetAccountInfo(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        this.Context.UpdateAccountInfo(accountInfo);

        return new List<MessageAccount>
        {
            this.CreateSpotAccount(accountInfo),
            this.CreateFuturesAccount(accountInfo)
        };
    }

    public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token)
    {
        var balances = this.HandleApiResponse(
            () => this.Api.PrivateRestApi.GetBalances(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        this.Context.UpdateBalances(balances);

        var result = balances.Select(this.CreateBalance).ToList();

        foreach (var message in result)
            this.sentBalancesCache[message.Key] = message;

        return result;
    }

    public override IList<MessageRule> GetRules(CancellationToken token) => new List<MessageRule>
    {
        new() { Name = Rule.ALLOW_SL, Value = false },
        new() { Name = Rule.ALLOW_TP, Value = false },
        new() { Name = Rule.PLACE_ORDER, Value = true },
        new() { Name = Rule.ALLOW_REDUCE_ONLY, Value = true }
    };

    #endregion Accounts and rules

    #region Orders

    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token)
    {
        IBalanceCalculator CreateBalanceCalculator(Symbol symbol)
        {
            return symbol.TypeSymbolType == SymbolType.Crypto
                ? new MexcSpotBalanceCalculator(this.Context)
                : new MexcFuturesBalanceCalculator(this.Context);
        }

        return new List<OrderType>
        {
            new MexcLimitOrderType(TimeInForce.Default, TimeInForce.GTT) { BalanceCalculator = CreateBalanceCalculator },
            new MexcMarketOrderType(TimeInForce.Default) { BalanceCalculatorFactory = CreateBalanceCalculator },
            new MexcStopOrderType(TimeInForce.Default, TimeInForce.GTT) { BalanceCalculatorFactory = CreateBalanceCalculator },
            new MexcStopLimitOrderType(TimeInForce.Default, TimeInForce.GTT) { BalanceCalculatorFactory = CreateBalanceCalculator }
        };
    }

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token)
    {
        var orders = this.TokenHandleApiResponse(
            () => this.Api.PrivateRestApi.GetOpenOrders(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        return orders.Select(this.CreateOpenOrder).ToList();
    }

    #endregion Orders

    #region Positions

    public override IList<MessageOpenPosition> GetPositions(CancellationToken token)
    {
        var positions = this.HandleApiResponse(
            () => this.Api.PrivateRestApi.GetPositions(token), token, out string error);

        if (!string.IsNullOrEmpty(error))
            throw new InvalidOperationException(error);

        this.Context.UpdatePositions(positions);

        return positions.Select(this.CreateOpenPosition).ToList();
    }

    public override PnL CalculatePnL(PnLRequestParameters parameters)
    {
        if (string.IsNullOrEmpty(parameters.PositionId) ||
            !this.Context.Positions.TryGetValue(parameters.PositionId, out var position))
            return base.CalculatePnL(parameters);

        return new PnL
        {
            NetPnL = new PnLItem
            {
                AssetID = MexcVendor.USER_ASSET_ID,
                Value = (double)position.UnrealizedProfit
            }
        };
    }

    #endregion Positions

    #region User trades

    public override TradesHistoryMetadata GetTradesMetadata() => new() { AllowLocalStorage = true };

    public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters)
    {
        var from = parameters.From;
        var to = parameters.To;
        var token = parameters.CancellationToken;

        const int LIMIT = 1000;

        var stack = new Stack<MexcTrade>();
        var result = new List<MessageTrade>();

        do
        {
            var trades = this.HandleApiResponse(
                () => this.Api.PrivateRestApi.GetUserTrades(from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), LIMIT, token), token, out _, true);

            if (trades == null)
                break;

            foreach (var trade in trades)
                stack.Push(trade);

            var lastTrade = trades.LastOrDefault();
            if (lastTrade == null)
                break;

            to = lastTrade.Time.AddMilliseconds(-1);
        }
        while (trades.Length == LIMIT && from < to && !token.IsCancellationRequested);

        while (stack.Any())
        {
            var trade = stack.Pop();
            result.Add(this.CreateTrade(trade));
        }

        return result;
    }

    #endregion User trades

    #region Trading operations

    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateSubmitOrderRequest(parameters);
            return this.Api.PrivateRestApi.PlaceOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateUpdateOrderRequest(parameters);
            return this.Api.PrivateRestApi.ModifyOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = new MexcCancelOrderRequest { OrderId = parameters.OrderId };
            return this.Api.PrivateRestApi.CancelOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters)
    {
        return this.MakeTradingOperation(() =>
        {
            var request = this.CreateClosePositionRequest(parameters);
            return this.Api.PrivateRestApi.PlaceOrder(request, parameters.CancellationToken);
        }, parameters.RequestId, parameters.CancellationToken);
    }

    private TradingOperationResult MakeTradingOperation(Func<Task<MexcOrderResponse>> tradingOperation, long requestId, CancellationToken token)
    {
        try
        {
            var orderResponse = this.HandleApiResponse(tradingOperation, token, out string error);

            if (!string.IsNullOrEmpty(error))
                return TradingOperationResult.CreateError(requestId, error);

            if (orderResponse == null)
                return TradingOperationResult.CreateError(requestId, "Order response is null");

            return TradingOperationResult.CreateSuccess(requestId, orderResponse.OrderId);
        }
        catch (Exception ex)
        {
            return TradingOperationResult.CreateError(requestId, ex.GetFullMessageRecursive());
        }
    }

    #endregion Trading operations

    #region Orders history

    public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters) =>
        this.LoadOrders(parameters.From, parameters.To, parameters.CancellationToken)
            .Select(this.CreateOrderHistory)
            .ToList();

    #endregion Orders history

    #region Reports

    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => new List<MessageReportType>
    {
        new()
        {
            Id = "orders_history",
            Name = loc._("Orders history"),
            Parameters = new List<SettingItem>
            {
                new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_FROM),
                new SettingItemDateTime(REPORT_TYPE_PARAMETER_DATETIME_TO)
            }
        }
    };

    public override Report GenerateReport(ReportRequestParameters request) => request.ReportType.Id switch
    {
        "orders_history" => this.GenerateOrdersHistoryReport(request),
        _ => null
    };

    private Report GenerateOrdersHistoryReport(ReportRequestParameters parameters)
    {
        var report = new Report();

        report.AddColumn(loc._("Order ID"), ComparingType.String);
        report.AddColumn(loc._("Symbol"), ComparingType.String);
        report.AddColumn(loc._("Creation time"), ComparingType.DateTime);
        report.AddColumn(loc._("Update time"), ComparingType.DateTime);
        report.AddColumn(loc._("Amount"), ComparingType.Double);
        report.AddColumn(loc._("Type"), ComparingType.String);
        report.AddColumn(loc._("Status"), ComparingType.String);
        report.AddColumn(loc._("Price"), ComparingType.Double);
        report.AddColumn(loc._("Average price"), ComparingType.Double);

        var settings = parameters.ReportType.Settings;
        var token = parameters.CancellationToken;

        var from = settings.GetValue<DateTime>(REPORT_TYPE_PARAMETER_DATETIME_FROM);
        var to = settings.GetValue<DateTime>(REPORT_TYPE_PARAMETER_DATETIME_TO);

        if (from == default || to == default)
            return report;

        var orders = this.LoadOrders(from, to, token);

        foreach (var order in orders)
        {
            var row = new ReportRow();
            row.AddCell(order.OrderId);
            row.AddCell(order.Symbol);
            row.AddCell(order.CreateTime, new DateTimeFormattingDescription(order.CreateTime));
            row.AddCell(order.UpdateTime, new DateTimeFormattingDescription(order.UpdateTime));
            row.AddCell(order.Quantity, new VolumeFormattingDescription(order.Quantity, order.Symbol));
            row.AddCell(order.Type);
            row.AddCell(order.Status);
            row.AddCell(order.Price, new PriceFormattingDescription(order.Price, order.Symbol));
            row.AddCell(order.AveragePrice, new PriceFormattingDescription(order.AveragePrice, order.Symbol));
            report.AddRow(row);
        }

        return report;
    }

    #endregion Reports

    #region Factory

    private MessageAccount CreateSpotAccount(MexcAccountInfo accountInfo) => new MessageCryptoAccount
    {
        AccountId = this.ConstructAccountId("SPOT"),
        AccountName = $"Spot Account",
        AccountAdditionalInfo = new List<AdditionalInfoItem>
        {
            new() { Id = nameof(AccountInfo.Balance), Hidden = true },
            new() { Id = nameof(AccountInfo.AccountCurrency), Hidden = true }
        }
    };

    private MessageAccount CreateFuturesAccount(MexcAccountInfo accountInfo) => new MessageCryptoAccount
    {
        AccountId = this.ConstructAccountId("FUTURES"),
        AccountName = "Futures Account",
        NettingType = NettingType.OnePosition,
        AccountAdditionalInfo = new List<AdditionalInfoItem>
        {
            new() { Id = nameof(AccountInfo.Balance), Hidden = true }
        }
    };

    private MessageCryptoAssetBalances CreateBalance(MexcBalance balance)
    {
        decimal totalBalance = balance.Free + balance.Locked;
        decimal totalBTC = 0;
        decimal totalUSD = 0;

        if (this.Context.CrossRates.TryGetCrossRate(balance.Asset, "BTC", out double rate))
            totalBTC = totalBalance * (decimal)rate;

        if (this.Context.CrossRates.TryGetCrossRate("BTC", "USDT", out rate))
            totalUSD = totalBTC * (decimal)rate;

        return new MessageCryptoAssetBalances
        {
            AccountId = this.ConstructAccountId(balance.AccountType),
            AssetId = balance.Asset,
            TotalBalance = (double)totalBalance,
            AvailableBalance = (double)balance.Free,
            ReservedBalance = (double)balance.Locked,
            TotalInBTC = (double)totalBTC,
            TotalInUSD = (double)totalUSD
        };
    }

    private MessageOpenOrder CreateOpenOrder(MexcOrder order)
    {
        var message = new MessageOpenOrder(order.Symbol);
        this.FillOrderMessage(message, order);
        return message;
    }

    private MessageOrderHistory CreateOrderHistory(MexcOrder order)
    {
        var message = new MessageOrderHistory(order.Symbol);
        this.FillOrderMessage(message, order);
        return message;
    }

    private void FillOrderMessage(MessageOpenOrder message, MexcOrder order)
    {
        message.AccountId = this.ConstructAccountId(order.Symbol.EndsWith("_PERP") ? "FUTURES" : "SPOT");
        message.OrderId = order.OrderId;
        message.LastUpdateTime = order.UpdateTime;
        message.FilledQuantity = order.QuantityFilled;
        message.TotalQuantity = order.Quantity;
        message.OrderTypeId = ConvertOrderType(order.Type);
        message.TimeInForce = TimeInForce.Default;
        message.Status = ConvertOrderStatus(order.Status);
        message.Side = order.Side == "BUY" ? Side.Buy : Side.Sell;
        message.AverageFillPrice = order.AveragePrice;

        switch (message.OrderTypeId)
        {
            case OrderType.Market:
                message.Price = order.Price;
                break;
            case OrderType.Limit:
                message.Price = order.Price;
                break;
            case OrderType.Stop:
                message.TriggerPrice = order.StopPrice;
                break;
            case OrderType.StopLimit:
                message.Price = order.Price;
                message.TriggerPrice = order.StopPrice;
                break;
        }

        message.AdditionalInfoItems = new List<AdditionalInfoItem>
        {
            new() { Id = MexcVendor.POST_ONLY, NameKey = "Post-only", Value = order.Type.Contains("POST_ONLY") }
        };
    }

    private static string ConvertOrderType(string orderType) => orderType switch
    {
        "MARKET" => OrderType.Market,
        "LIMIT" or "LIMIT_MAKER" => OrderType.Limit,
        "STOP" => OrderType.Stop,
        "STOP_LIMIT" => OrderType.StopLimit,
        _ => throw new ArgumentOutOfRangeException(nameof(orderType))
    };

    private static OrderStatus ConvertOrderStatus(string status) => status switch
    {
        "NEW" => OrderStatus.Opened,
        "CANCELED" => OrderStatus.Cancelled,
        "FILLED" => OrderStatus.Filled,
        "PARTIALLY_FILLED" => OrderStatus.PartiallyFilled,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private MessageOpenPosition CreateOpenPosition(MexcPosition position) => new MessageOpenPosition(position.Symbol)
    {
        AccountId = this.ConstructAccountId("FUTURES"),
        Side = position.Side == "LONG" ? Side.Buy : Side.Sell,
        PositionId = position.Symbol,
        Quantity = Math.Abs(position.PositionAmt),
        OpenPrice = position.EntryPrice,
        OpenTime = DateTime.UtcNow
    };

    private MessageTrade CreateTrade(MexcTrade trade) => new MessageTrade
    {
        TradeId = trade.Id,
        SymbolId = trade.Symbol,
        AccountId = this.ConstructAccountId(trade.Symbol.EndsWith("_PERP") ? "FUTURES" : "SPOT"),
        Price = trade.Price,
        Quantity = trade.Quantity,
        DateTime = trade.Time,
        Side = trade.Side == "BUY" ? Side.Buy : Side.Sell,
        OrderId = trade.OrderId
    };

    private MexcSubmitOrderRequest CreateSubmitOrderRequest(OrderRequestParameters parameters)
    {
        var request = new MexcSubmitOrderRequest
        {
            Symbol = parameters.SymbolId,
            Side = parameters.Side == Side.Buy ? "BUY" : "SELL",
            Type = parameters.OrderTypeId switch
            {
                OrderType.Market => "MARKET",
                OrderType.Limit => parameters.IsPostOnly() ? "LIMIT_MAKER" : "LIMIT",
                OrderType.Stop => "STOP",
                OrderType.StopLimit => "STOP_LIMIT",
                _ => throw new InvalidOperationException("Unsupported order type")
            },
            Quantity = parameters.Quantity.ToString(CultureInfo.InvariantCulture)
        };

        if (parameters.OrderType == OrderType.Limit)
            request.Price = parameters.Price.FormatPrice();

        if (parameters.OrderType == OrderType.Stop || parameters.OrderType == OrderType.StopLimit)
            request.StopPrice = parameters.TriggerPrice.FormatPrice();

        return request;
    }

    private MexcUpdateOrderRequest CreateUpdateOrderRequest(ModifyOrderRequestParameters parameters)
    {
        var request = new MexcUpdateOrderRequest
        {
            OrderId = parameters.OrderId,
            Symbol = parameters.SymbolId,
            Side = parameters.Side == Side.Buy ? "BUY" : "SELL",
            Quantity = parameters.Quantity.ToString(CultureInfo.InvariantCulture)
        };

        if (parameters.OrderType == OrderType.Limit)
            request.Price = parameters.Price.FormatPrice();

        if (parameters.OrderType == OrderType.Stop || parameters.OrderType == OrderType.StopLimit)
            request.StopPrice = parameters.TriggerPrice;

        return request;
    }

    private MexcSubmitOrderRequest CreateClosePositionRequest(ClosePositionRequestParameters parameters)
    {
        return new MexcSubmitOrderRequest
        {
            Symbol = parameters.Position.Symbol,
            Side = parameters.Position.Side == Side.Buy ? "SELL" : "BUY",
            Type = "MARKET",
            Quantity = parameters.CloseQuantity.ToString(CultureInfo.InvariantCulture),
            ReduceOnly = true
        };
    }

    #endregion Factory

    #region Misc

    private string ConstructAccountId(string accountType) => $"{accountType}_{this.Api.UserId}";

    private IEnumerable<MexcOrder> LoadOrders(DateTime from, DateTime to, CancellationToken cancellation)
    {
        const int LIMIT = 1000;

        var stack = new Stack<MexcOrder>();

        do
        {
            var orders = this.HandleApiResponse(
                () => this.Api.PrivateRestApi.GetOrderHistory(from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), LIMIT, cancellation), cancellation, out _, true);

            if (orders == null)
                break;

            foreach (var order in orders)
                stack.Push(order);

            var lastOrder = orders.LastOrDefault();
            if (lastOrder == null)
                break;

            to = lastOrder.CreateTime.AddMilliseconds(-1);
        }
        while (orders.Length == LIMIT && from < to && !cancellation.IsCancellationRequested);

        while (stack.Any())
            yield return stack.Pop();
    }

    #endregion Misc

    #region Periodic actions

    private void UpdateBalancesAction()
    {
        var balances = this.HandleApiResponse(
            () => this.Api.PrivateRestApi.GetBalances(this.GlobalCancellation), this.GlobalCancellation, out string error);

        if (!string.IsNullOrEmpty(error))
        {
            Core.Instance.Loggers.Log($"{nameof(this.UpdateBalancesAction)}: {error}", LoggingLevel.Error, MexcVendor.VENDOR_NAME);
            return;
        }

        this.Context.UpdateBalances(balances);

        foreach (var message in balances.Select(this.CreateBalance))
        {
            if (this.sentBalancesCache.TryGetValue(message.Key, out var sentMessage) && sentMessage.Equals(message))
                continue;

            this.PushMessage(message);
            this.sentBalancesCache[message.Key] = message;
        }
    }

    #endregion Periodic actions

    private void PrivateWebSocketApiOnNewData(object sender, MexcEventArgs e)
    {
        if (e.Balance != null)
        {
            this.Context.UpdateBalances(e.Balance);
            this.PushMessage(this.CreateBalance(e.Balance));
        }

        if (e.Order != null)
        {
            var orderHistory = this.CreateOrderHistory(e.Order);
            this.PushMessage(orderHistory);

            if (orderHistory.Status is OrderStatus.Cancelled or OrderStatus.Filled)
                this.PushMessage(new MessageCloseOrder { OrderId = e.Order.OrderId });
            else
                this.PushMessage(this.CreateOpenOrder(e.Order));
        }

        if (e.Position != null)
        {
            this.PushMessage(this.CreateOpenPosition(e.Position));
            this.Context.UpdatePositions(e.Position);
        }

        if (e.Trade != null)
            this.PushMessage(this.CreateTrade(e.Trade));
    }

    protected override void OnTimerTick()
    {
        base.OnTimerTick();

        this.tickersUpdateTask ??= Task.Run(this.UpdateTickersAction)
            .ContinueWith(_ => this.tickersUpdateTask = null);

        this.balancesUpdateTask ??= Task.Run(this.UpdateBalancesAction)
            .ContinueWith(_ => this.balancesUpdateTask = null);
    }
}
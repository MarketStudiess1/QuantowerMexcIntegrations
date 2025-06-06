using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;

namespace MexcVendor;

public class MexcVendor : Vendor
{
    #region Consts

    internal const string VENDOR_NAME = "MEXC";
    internal const int EXCHANGE_ID = 1;
    internal const string TRADING_INFO_GROUP = "#20.Trading info";
    internal const string ACCOUNT_INFO_GROUP = "#80.Info";

    private const string CONNECTION_INFO = "Info";
    private const string CONNECTION_TRADING = "Trading";

    internal const string PARAMETER_API_KEY = "apiKey";
    internal const string PARAMETER_SECRET_KEY = "secretKey";

    internal const string USER_ASSET_ID = "USDT";

    internal const string HIDDEN = "hidden";
    internal const string POST_ONLY = "postOnly";
    internal const string LEVERAGE = "leverage";
    internal const string CLIENT_ORDER_ID = "clientOrderId";

    #endregion Consts

    #region Integration details

    public static VendorMetaData GetVendorMetaData() => new()
    {
        VendorName = VENDOR_NAME,
        VendorDescription = loc._("Market data and trading for MEXC"),
        GetDefaultConnections = () => new List<ConnectionInfo>
        {
            CreateDefaultConnectionInfo("MEXC", VENDOR_NAME, "MexcVendor\\MEXC.png", links: new List<ConnectionInfoLink>
            {
                new() { Title = "Register account", Url = "https://www.mexc.com/register" }
            }
        },
        GetConnectionParameters = () =>
        {
            var infoItem = new SelectItem(CONNECTION_INFO, CONNECTION_INFO);
            var tradingItem = new SelectItem(CONNECTION_TRADING_ITEM, CONNECTION_TRADING);

            var relation = new SettingItemRelationEnability(CONNECTION, tradingItem);

            return new List<SettingItem>
            {
                new SettingItemGroup(LOGIN_PARAMETER_GROUP, new List<SettingItem>
                {
                    new SettingItemRadioLocalized(CONNECTION_TYPE, infoItem, new List<SelectItem> { infoItem, tradingItem }),
                    new SettingItemPassword(PARAMETER_API_KEY, new PasswordHolder)
                    {
                        Text = loc._("API key"),
                        Relation = relation
                    },
                    new SettingItemPassword(PARAMETER_SECRET_KEY, new PasswordHolder)
                    {
                        Text = loc._("Secret key"),
                        Relation = relation
                    }
                })
            };
        }
    #endregion Integration details

        #region Properties

    private Vendor vendor;

    #endregion Properties

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        if (!NetworkInterface.GetIsNetwork())
            return ConnectionResult.CreateFail(loc._("Network does not available"));

        var settingItem = parameters.ConnectionSettings.GetItemByPath(LOGIN_PARAMETER_GROUP, CONNECTION);
        if (settingItem is not { Value: SelectItem })
            return ConnectionResult.CreateFail("Cannot find connection parameters");

        this.vendor = selectItem.Value.ToString() == CONNECTION_INFO
            ? new MexcMarketDataVendor()
            : new MexcTradingVendor();

        this.vendor.NewMessage += this.OnVendorMessage;

        return this.vendor.Connect(parameters);
    }

    public override void OnConnected(CancellationToken token) => this.vendor.OnConnected(token);

    public override void Disconnect() => this.vendor.Disconnect();

    public override PingResult Ping() => this.vendor.Ping();

    #endregion

    #region Accounts and rules

    public override IList<MessageAccount> GetAccounts(CancellationToken token) => this.vendor.GetAccounts(token);

    public override IList<MessageCryptoAssetBalances> GetCryptoAssetBalances(CancellationToken token) => this.vendor.GetCryptoAssetBalances(token);

    public override IList<MessageRule> GetRules(CancellationToken token) => this.vendor.GetRules(token);

    public override IList<MessageAccountOperation> GetAccountOperations(CancellationToken token) => this.vendor.GetAccountOperations(token);

    #endregion

    #region Symbols and symbol groups

    public override IList<MessageSymbol> GetSymbols(CancellationToken token) => this.vendor.GetSymbols(token);

    public override MessageSymbolTypes GetSymbolTypes(CancellationToken token) => this.vendor.GetSymbolTypes(token);

    public override IList<MessageAsset> GetAssets(CancellationToken token) => this.vendor.GetAssets(token);

    public override IList<MessageExchange> GetExchanges(CancellationToken token) => this.vendor.GetExchanges(token);

    #endregion

    #region Subscriptions

    public override void SubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.SubscribeSymbol(parameters);

    public override void UnSubscribeSymbol(SubscribeQuotesParameters parameters) => this.vendor.UnSubscribeSymbol(parameters);

    #endregion Subscriptions

    #region Orders and positions

    public override IList<OrderType> GetAllowedOrderTypes(CancellationToken token) => this.vendor.GetAllowedOrderTypes(token);

    public override IList<MessageOpenOrder> GetPendingOrders(CancellationToken token) => this.vendor.GetPendingOrders(token);

    public override IList<MessageOpenPosition> GetPositions(CancellationToken token) => this.vendor.GetPositions(token);

    public override PnL CalculatePnL(PnLRequestParameters parameters) => this.vendor.CalculatePnL(parameters);

    #endregion Orders and positions

    #region Trading operations: placing, modifying, cancelling orders

    public override TradingOperationResult PlaceOrder(PlaceOrderRequestParameters request) => this.vendor.PlaceOrder(request);

    public override TradingOperationResult ModifyOrder(ModifyOrderRequestParameters request) => this.vendor.ModifyOrder(request);

    public override TradingOperationResult CancelOrder(CancelOrderRequestParameters request) => this.vendor.CancelOrder(request);

    public override TradingOperationResult ClosePosition(ClosePositionRequestParameters parameters) => this.vendor.ClosePosition(parameters);

    public override MarginInfo GetMarginInfo(OrderRequestParameters orderRequestParameters) => this.vendor.GetMarginInfo(orderRequestParameters);

    #endregion Trading operations: placing, modifying, cancelling orders

    #region History

    public override HistoryMetadata GetHistoryMetadata(CancellationToken cancellation) => this.vendor.GetHistoryMetadata(cancellation);

    public override IList<IHistoryItem> LoadHistory(HistoryRequestParameters requestParameters) => this.vendor.LoadHistory(requestParameters);

    #endregion History

    #region Volume analysis

    public override VolumeAnalysisMetadata GetVolumeAnalysisMetadata() => this.vendor.GetVolumeAnalysisMetadata();

    public override VendorVolumeAnalysisByPeriodResponse LoadVolumeAnalysis(VolumeAnalysisByPeriodRequestParameters requestParameters)
        => this.vendor.LoadVolumeAnalysis(requestParameters);

    #endregion Volume analysis

    #region Trades history

    public override TradesHistoryMetadata GetTradesMetadata() => this.vendor.GetTradesMetadata();

    public override IList<MessageTrade> GetTrades(TradesHistoryRequestParameters parameters) => this.vendor.GetTrades(parameters);

    #endregion Trades history

    #region Orders history

    public override IList<MessageOrderHistory> GetOrdersHistory(OrdersHistoryRequestParameters parameters) => this.vendor.GetOrdersHistory(parameters);

    #endregion Orders history

    #region Reports

    public override IList<MessageReportType> GetReportsMetaData(CancellationToken token) => this.vendor.GetReportsMetaData(token);

    public override Report GenerateReport(ReportRequestParameters reportRequestParameters) => this.vendor.GenerateReport(reportRequestParameters);

    #endregion Reports

    private void OnVendorMessage(object sender, VendorEventArgs e) => this.PushMessage(e.Message);
}

using Mexc.API.Models;
using MexcVendor.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor.Misc;

public class MexcContext : IDisposable
{
    #region Properties

    public IDictionary<string, string> AssetLabels { get; }

    public IDictionary<string, MexcSymbolDetails> Symbols { get; }

    public HashSet<string> Futures { get; }

    public IDictionary<string, MexcTicker> Tickers { get; }

    public IDictionary<string, long> LastTradeTimes { get; }

    public CrossRateCache CrossRates { get; }

    public IDictionary<MexcBalanceKey, MexcBalance> Balances { get; }

    public IDictionary<string, MexcPosition> Positions { get; }

    public MexcAccountInfo AccountInfo { get; internal set; }

    private static readonly Regex futuresAssetRegex;

    #endregion Properties

    static MexcContext()
    {
        futuresAssetRegex = new Regex(@"([A-Z0-9]+)_PERP$");
    }

    public MexcContext()
    {
        this.AssetLabels = new Dictionary<string, string>();
        this.Symbols = new Dictionary<string, MexcSymbolDetails>();
        this.Futures = new HashSet<string>();
        this.Tickers = new Dictionary<string, MexcTicker>();
        this.LastTradeTimes = new Dictionary<string, long>();
        this.CrossRates = new CrossRateCache();
        this.Balances = new Dictionary<MexcBalanceKey, MexcBalance>();
        this.Positions = new Dictionary<string, MexcPosition>();
    }

    public void Dispose()
    {
        this.AssetLabels.Clear();
        this.Symbols.Clear();
        this.Futures.Clear();
        this.Tickers.Clear();
        this.LastTradeTimes.Clear();
        this.CrossRates.Dispose();
        this.Balances.Clear();
        this.Positions.Clear();
    }

    public static bool TryParseAssets(string pair, out string baseAsset, out string quoteAsset)
    {
        baseAsset = default;
        quoteAsset = default;

        if (string.IsNullOrEmpty(pair))
            return false;

        // Ejemplo: BTCUSDT
        var match = futuresAssetRegex.Match(pair);
        if (match.Success)
        {
            baseAsset = match.Groups[1].Value;
            quoteAsset = "USDT";
            return true;
        }

        // Para pares de spot (BTCUSDT) o futuros (BTCUSDT_PERP)
        int separatorIndex = pair.IndexOf('_');
        if (separatorIndex >= 0)
        {
            baseAsset = pair.Substring(0, separatorIndex);
            quoteAsset = pair.Substring(separatorIndex + 1).Replace("_PERP", "");
            return true;
        }

        // Asumir que el par es continuo (por ejemplo, BTCUSDT)
        if (pair.EndsWith("USDT"))
        {
            quoteAsset = "USDT";
            baseAsset = pair.Substring(0, pair.Length - 4);
            return true;
        }
        else if (pair.EndsWith("BTC"))
        {
            quoteAsset = "BTC";
            baseAsset = pair.Substring(0, pair.Length - 3);
            return true;
        }

        return false;
    }

    public static string GetAssetName(string asset)
    {
        var match = futuresAssetRegex.Match(asset);
        if (!match.Success || match.Groups.Count < 2)
            return asset;

        return match.Groups[1].Value;
    }

    public int GetSymbolPrecision(string pair)
    {
        if (!this.Tickers.TryGetValue(pair, out var ticker))
            return 8;

        return ticker.LastPrice switch
        {
            < 0.1m => 6,
            < 1m => 5,
            < 10m => 4,
            < 100m => 3,
            < 1000m => 2,
            < 10000m => 1,
            _ => 0
        };
    }

    public void UpdateTickers(MexcTicker[] tickers)
    {
        foreach (var ticker in tickers)
        {
            this.Tickers[ticker.Pair] = ticker;

            if (!TryParseAssets(ticker.Pair, out string baseAsset, out string quoteAsset))
                continue;

            this.CrossRates.ProcessPrice(baseAsset, quoteAsset, (double)ticker.LastPrice);
        }
    }

    public void UpdateBalances(params MexcBalance[] balances)
    {
        foreach (var balance in balances)
            this.Balances[balance.GetKey()] = balance;
    }

    public void UpdatePositions(params MexcPosition[] positions)
    {
        foreach (var position in positions)
        {
            if (position.PositionAmt == 0m) // Posición cerrada
                this.Positions.Remove(position.Symbol);
            else
                this.Positions[position.Symbol] = position;
        }
    }

    public void UpdateAccountInfo(MexcAccountInfo accountInfo) => this.AccountInfo = accountInfo;
}
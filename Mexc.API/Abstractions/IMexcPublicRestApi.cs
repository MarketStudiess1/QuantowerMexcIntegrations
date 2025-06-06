
using System.Threading;
using System.Threading.Tasks;
using Mexc.API.Models;
using Refit;

namespace Mexc.API.Abstractions;

public interface IMexcPublicRestApi
{
    [Get("/api/v3/klines?symbol={symbol}&interval={interval}&startTime={startTime}&endTime={endTime}&limit={limit}")]
    Task<MexcCandle[]> GetCandles(string symbol, string interval, long? startTime, long? endTime, int? limit, CancellationToken cancellation);

    [Get("/api/v3/ticker/24hr")]
    Task<MexcTicker[]> GetTickers(CancellationToken cancellation);

    [Get("/api/v3/trades?symbol={symbol}&limit={limit}")]
    Task<MexcTrade[]> GetTrades(string symbol, int? limit, CancellationToken cancellation);

    [Get("/api/v3/exchangeInfo")]
    Task<MexcExchangeInfo> GetExchangeInfo(CancellationToken cancellation);
}
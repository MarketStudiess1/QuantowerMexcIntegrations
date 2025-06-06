
using System.Threading;
using System.Threading.Tasks;
using Mexc.API.Models;
using Mexc.API.Models.Requests;
using Refit;

namespace Mexc.API.Abstractions;

public interface IMexcPrivateRestApi
{
    [Get("/api/v3/account")]
    Task<MexcAccountInfo> GetAccountInfo([Query] MexcAccountInfoRequest request, CancellationToken cancellation);

    [Post("/api/v3/order")]
    Task<MexcOrderResponse> PlaceOrder([Body] MexcPlaceOrderRequest request, CancellationToken cancellation);

    [Delete("/api/v3/order")]
    Task<MexcOrderResponse> CancelOrder([Query] MexcCancelOrderRequest request, CancellationToken cancellation);

    [Get("/api/v3/order")]
    Task<MexcOrder> QueryOrder([Query] MexcQueryOrderRequest request, CancellationToken cancellation);

    [Get("/api/v3/myTrades")]
    Task<MexcUserTrade[]> GetUserTrades([Query] MexcUserTradesRequest request, CancellationToken cancellation);
}
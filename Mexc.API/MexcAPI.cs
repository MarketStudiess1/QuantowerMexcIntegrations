
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Mexc.API.Abstractions;
using Mexc.API.JsonConverters;
using Mexc.API.Misc;
using Newtonsoft.Json;
using Refit;

namespace Mexc.API;

public class MexcApi : IDisposable
{
    public IMexcPublicRestApi PublicRestApi { get; }
    public IMexcPrivateRestApi PrivateRestApi { get; }
    public IMexcPublicWebSocketApi PublicWebSocketApi { get; }
    public IMexcPrivateWebSocketApi PrivateWebSocketApi { get; }

    public bool IsConnected => this.PublicWebSocketApi.IsOpened && (this.PrivateWebSocketApi?.IsOpened ?? true);

    private readonly MexcAuthHelper authHelper;

    public MexcApi(CancellationToken cancellation)
    {
        var settings = new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new MexcCandleJsonConverter(),
                new MexcTickerJsonConverter(),
                new MexcUserTradeJsonConverter()
            }
        }));

        this.PublicRestApi = RestService.For<IMexcPublicRestApi>("https://api.mexc.com/api/v3", settings);
        this.PublicWebSocketApi = new MexcPublicWebSocketApi("wss://wbs.mexc.com/ws");
        this.PublicWebSocketApi.Connect(cancellation);
    }

    public MexcApi(string apiKey, string apiSecret, CancellationToken cancellation)
        : this(cancellation)
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentNullException(nameof(apiKey));

        if (string.IsNullOrEmpty(apiSecret))
            throw new ArgumentNullException(nameof(apiSecret));

        this.authHelper = new MexcAuthHelper();

        this.PrivateRestApi = RestService.For<IMexcPrivateRestApi>(new HttpClient(new MexcPrivateHttpRequestHandler(apiKey, apiSecret, this.authHelper))
        {
            BaseAddress = new Uri("https://api.mexc.com")
        }, new RefitSettings(new NewtonsoftJsonContentSerializer(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new MexcOrderJsonConverter()
            }
        })));

        this.PrivateWebSocketApi = new MexcPrivateWebSocketApi("wss://wbs-api.mexc.com/api/v2/private", apiKey, apiSecret, this.authHelper);
        this.PrivateWebSocketApi.Connect(cancellation);
        string error = this.PrivateWebSocketApi.Authenticate();
        if (!string.IsNullOrEmpty(error))
            throw new AuthenticationException(error);
    }

    public void Dispose()
    {
        this.PublicWebSocketApi.Disconnect();
        this.PrivateWebSocketApi?.Disconnect();
    }

    private class MexcPrivateHttpRequestHandler : HttpClientHandler
    {
        private readonly string apiKey, apiSecret;
        private readonly MexcAuthHelper authHelper;

        public MexcPrivateHttpRequestHandler(string apiKey, string apiSecret, MexcAuthHelper authHelper)
        {
            this.apiKey = apiKey;
            this.apiSecret = apiSecret;
            this.authHelper = authHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            long timestamp = authHelper.GenerateTimestamp();
            string queryString = await MexcAuthHelper.GetQueryString(request);
            string signature = MexcAuthHelper.ComputeSignature(queryString, this.apiSecret);

            request.Headers.Add("X-MEXC-APIKEY", this.apiKey);
            request.RequestUri = new Uri($"{request.RequestUri}?{queryString}&signature={signature}");

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
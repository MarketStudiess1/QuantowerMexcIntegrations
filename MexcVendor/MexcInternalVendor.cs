using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Mexc.API;
using Mexc.API.Models;
using MexcVendor.Misc;
using Newtonsoft.Json;
using Refit;
using TradingPlatform.BusinessLayer;
using TradingPlatform.BusinessLayer.Integration;
using TradingPlatform.BusinessLayer.Utils;

namespace MexcVendor;

internal class MexcInternalVendor : Vendor
{
    #region Properties

    protected MexcApi Api { get; private set; }

    protected MexcContext Context { get; }

    private readonly PingMeter pingMeter;

    protected CancellationToken GlobalCancellation => this.globalCancellation.Token;
    private CancellationTokenSource globalCancellation;

    private Timer timer;

    private readonly DealTicketLimiter tooManyRequestsLimiter;

    #endregion Properties

    protected MexcInternalVendor()
    {
        this.Context = new MexcContext();
        this.pingMeter = new PingMeter(MexcVendor.VENDOR_NAME, "https://api.mexc.com");
        this.tooManyRequestsLimiter = new DealTicketLimiter(TimeSpan.FromSeconds(15));
    }

    #region Connection

    public override ConnectionResult Connect(ConnectRequestParameters parameters)
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return ConnectionResult.CreateFail(loc._("Network does not available"));

        this.Api = this.CreateApiClient(parameters);

        if (!this.Api.IsConnected)
            return ConnectionResult.CreateFail(loc._("Can't connect."));

        return ConnectionResult.CreateSuccess();
    }

    protected virtual MexcApi CreateApiClient(ConnectRequestParameters parameters) => new(parameters.CancellationToken);

    public override void OnConnected(CancellationToken token)
    {
        this.globalCancellation = new CancellationTokenSource();
        this.timer = new Timer(this.TimerCallback, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        base.OnConnected(token);
    }

    public override void Disconnect()
    {
        if (this.timer != null)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.timer.Dispose();
        }

        this.globalCancellation?.Cancel();
        this.Api?.Dispose();
        this.Context?.Dispose();
        base.Disconnect();
    }

    public override PingResult Ping()
    {
        var result = new PingResult { State = PingEnum.Disconnected };

        if (this.Api is not { IsConnected: true })
            return result;

        try
        {
            var stopWatch = Stopwatch.StartNew();
            this.Api.PublicRestApi.Ping(this.GlobalCancellation);
            stopWatch.Stop();

            result.RoundTripTime = stopWatch.Elapsed;
            result.PingTime = this.pingMeter.MeasurePing();
            result.State = result.PingTime != null ? PingEnum.Connected : PingEnum.Disconnected;
        }
        catch (Exception ex)
        {
            Core.Instance.Loggers.Log(ex, nameof(this.Ping));
        }

        return result;
    }

    #endregion Connection

    #region Misc

    protected TResult HandleApiResponse<TResult>(Func<Task<TResult>> taskFunc, CancellationToken token, out string error, bool notify = false, bool retry = false, int iteration = 0)
    {
        error = default;

        try
        {
            return taskFunc.Invoke().GetResultWithoutContextCapturing();
        }
        catch (ApiException aex)
        {
            try
            {
                var mexcError = JsonConvert.DeserializeObject<MexcError>(aex.Content);
                if (mexcError != null)
                {
                    if (mexcError.Code == -1021) // Timestamp for this request is outside of the recvWindow
                    {
                        if (iteration >= 10)
                            error = mexcError.Msg;
                        else
                        {
                            this.Api.AdjustTimestampOffset();
                            return this.HandleApiResponse(taskFunc, token, out error, notify, retry, iteration + 1);
                        }
                    }
                    error = mexcError.Msg;
                }
            }
            catch
            {
                error = $"{aex.RequestMessage.RequestUri.LocalPath}. {aex.GetFullMessageRecursive()}";
            }

            if ((int)aex.StatusCode == 429)
            {
                if (notify)
                    notify = this.tooManyRequestsLimiter.AllowDealTicket();

                if (notify && !string.IsNullOrEmpty(error))
                {
                    this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));
                    notify = false;
                }

                if (retry && aex.Headers.TryGetValues("Retry-After", out var values))
                {
                    string valueString = values.FirstOrDefault();
                    if (!string.IsNullOrEmpty(valueString) && int.TryParse(valueString, out int value))
                    {
                        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, this.GlobalCancellation);
                        Task.Delay(TimeSpan.FromSeconds(value), linkedCts.Token).Wait(linkedCts.Token);
                        return this.HandleApiResponse(taskFunc, token, out _, true, true);
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
        catch (Exception ex)
        {
            error = ex.GetFullMessageRecursive();
        }

        if (notify && !string.IsNullOrEmpty(error))
            this.PushMessage(MessageDealTicket.CreateRefuseDealTicket(error));

        return default;
    }

    #endregion Misc

    private void TimerCallback(object state)
    {
        try
        {
            this.OnTimerTick();
        }
        catch (Exception ex)
        {
            Core.Instance.Loggers.Log(ex, nameof(this.TimerCallback));
        }
    }

    protected virtual void OnTimerTick() { }
}
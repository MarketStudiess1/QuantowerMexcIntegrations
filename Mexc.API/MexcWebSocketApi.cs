// Copyright [Your Name] © 2025.

using System;
using System.Collections.Generic;
using System.Threading;
using Mexc.API.Abstractions;
using Mexc.API.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocket4Net;

namespace Mexc.API;

internal abstract class MexcWebSocketApi : IMexcWebSocketApi
{
    public bool IsOpened => webSocket?.State == WebSocketState.Open;

    public event EventHandler<MexcEventArgs> NewData;
    public event EventHandler<MexcErrorEventArgs> Error;

    private readonly WebSocket webSocket;
    private ManualResetEventSlim connectEvent;
    private JObject responseMessage;
    private ManualResetEventSlim responseEvent;
    private readonly object sendLocker;
    private Exception lastError;
    private readonly TimeSpan timeout = TimeSpan.FromSeconds(30);

    protected MexcWebSocketApi(string endpoint)
    {
        this.webSocket = new WebSocket(endpoint);
        this.webSocket.Opened += WebSocketOnOpened;
        this.webSocket.MessageReceived += WebSocketOnMessageReceived;
        this.webSocket.Error += WebSocketOnError;
        this.webSocket.Closed += WebSocketOnClosed;
        this.sendLocker = new object();
    }

    public void Connect(CancellationToken cancellation)
    {
        connectEvent = new ManualResetEventSlim();
        webSocket.OpenAsync();
        Wait(connectEvent, cancellation);
        if (webSocket.State != WebSocketState.Open && lastError != null)
            throw lastError;
    }

    public void Disconnect()
    {
        webSocket.Close("disconnect");
    }

    public void Ping(CancellationToken cancellation)
    {
        var message = new { method = "PING" };
        SendMessage(JsonConvert.SerializeObject(message), cancellation);
    }

    protected Dictionary<string, object> SendMessage(string message, CancellationToken cancellation)
    {
        lock (sendLocker)
        {
            try
            {
                responseEvent = new ManualResetEventSlim();
                SendText(message);
                Wait(responseEvent, cancellation);
                return responseMessage?.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
            }
            finally
            {
                responseEvent = null;
            }
        }
    }

    private void SendText(string text)
    {
        webSocket.Send(text);
        DebugLog($">>>>> {text}");
    }

    protected abstract void DeserializeData(JObject message);

    private void Wait(ManualResetEventSlim eventSlim, CancellationToken cancellation)
    {
        int waitResult = WaitHandle.WaitAny(new[]
        {
            eventSlim.WaitHandle,
            cancellation.WaitHandle,
            new CancellationTokenSource(timeout).Token.WaitHandle
        });

        if (waitResult == 2)
            throw new TimeoutException();
    }

    protected void OnNewData(MexcEventArgs eventArgs) => NewData?.Invoke(this, eventArgs);
    protected void OnError(MexcErrorEventArgs eventArgs) => Error?.Invoke(this, eventArgs);

    private void WebSocketOnOpened(object sender, EventArgs e) => connectEvent?.Set();

    private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        DebugLog($"<<<<< {e.Message}");
        try
        {
            var jToken = JToken.Parse(e.Message);
            if (jToken is JObject jObject)
            {
                if (responseEvent != null)
                {
                    responseMessage = jObject;
                    responseEvent.Set();
                }
                DeserializeData(jObject);
            }
        }
        catch (Exception ex)
        {
            OnError(new MexcErrorEventArgs { Exception = ex });
        }
    }

    private void WebSocketOnError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
    {
        lastError = e.Exception;
        connectEvent?.Set();
        OnError(new MexcErrorEventArgs { Exception = e.Exception });
    }

    private void WebSocketOnClosed(object sender, EventArgs e) => connectEvent?.Set();

    private static void DebugLog(string text) => System.Diagnostics.Debug.WriteLine(text);
}
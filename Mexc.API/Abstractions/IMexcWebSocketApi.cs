
using System;
using System.Threading;
using Mexc.API.Models;

namespace Mexc.API.Abstractions;

public interface IMexcWebSocketApi
{
    bool IsOpened { get; }
    event EventHandler<MexcEventArgs> NewData;
    event EventHandler<MexcErrorEventArgs> Error;
    void Connect(CancellationToken cancellation);
    void Disconnect();
    void Ping(CancellationToken cancellation);
}

using System.Threading;

namespace Mexc.API.Abstractions;

public interface IMexcPublicWebSocketApi : IMexcWebSocketApi
{
    void Subscribe(string channel, string symbol, CancellationToken cancellation, out string error);
    void Unsubscribe(string channel, string symbol, CancellationToken cancellation, out string error);
}
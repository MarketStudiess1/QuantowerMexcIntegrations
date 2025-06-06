
namespace Mexc.API.Abstractions;

public interface IMexcPrivateWebSocketApi : IMexcWebSocketApi
{
    string Authenticate();
}
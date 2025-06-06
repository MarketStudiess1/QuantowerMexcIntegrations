
using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Mexc.API.Misc;

internal class MexcAuthHelper
{
    public long GenerateTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static async Task<string> GetQueryString(HttpRequestMessage request)
    {
        string query = request.RequestUri.Query.TrimStart('?');
        string body = request.Content != null ? await request.Content.ReadAsStringAsync() : "";
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"{query}{(query.Length > 0 ? "&" : "")}timestamp={timestamp}{(body.Length > 0 ? "&" + body : "")}";
    }

    public static string ComputeSignature(string payload, string apiSecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var builder = new StringBuilder();
        foreach (byte b in hash)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }
}
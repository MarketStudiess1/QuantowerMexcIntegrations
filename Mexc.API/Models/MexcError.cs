
using Mexc.API.JsonConverters;
using Newtonsoft.Json;

namespace Mexc.API.Models;

[JsonConverter(typeof(MexcErrorJsonConverter))]
public class MexcError
{
    public int Code { get; internal set; } // Error code, e.g., -1121
    public string Message { get; internal set; } // Error message

    public override string ToString() => $"{this.Code}: {this.Message}";
}
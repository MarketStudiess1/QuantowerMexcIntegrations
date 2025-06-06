
using System.Runtime.Serialization;

namespace Mexc.API.Models.Requests;

[DataContract]
public class MexcUserTradesRequest : MexcRangeRequest
{
    [DataMember(Name = "symbol")]
    public string Symbol { get; set; } // Trading pair
}
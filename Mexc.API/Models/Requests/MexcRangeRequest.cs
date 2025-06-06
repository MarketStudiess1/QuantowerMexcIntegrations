
using System.Runtime.Serialization;
using Refit;

namespace Mexc.API.Models.Requests;

[DataContract]
public abstract class MexcRangeRequest
{
    [DataMember(Name = "startTime")]
    [AliasAs("startTime")]
    public long? StartTime { get; set; } // Start time in milliseconds

    [DataMember(Name = "endTime")]
    [AliasAs("endTime")]
    public long? EndTime { get; set; } // End time in milliseconds

    [DataMember(Name = "limit")]
    [AliasAs("limit")]
    public int? Limit { get; set; } // Max number of results
}
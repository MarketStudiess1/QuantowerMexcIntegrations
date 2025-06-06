
using System;

namespace Mexc.API.Models;

public class MexcAccountInfo
{
    public string AccountType { get; set; } // SPOT o FUTURES
    public bool CanTrade { get; set; }
    public bool CanWithdraw { get; set; }
    public bool CanDeposit { get; set; }
    public long UpdateTime { get; set; }
}
using System;

namespace Mexc.API.Models;

/// <summary>
/// Event arguments for WebSocket errors from MEXC API.
/// </summary>
public class MexcErrorEventArgs : EventArgs
{
    public Exception Exception { get; internal set; } // Exception details
}
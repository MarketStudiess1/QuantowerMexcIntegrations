using System;

namespace Mexc.API.Models;

public class MexcPosition
{
    public string Symbol { get; set; }
    public decimal PositionAmt { get; set; } // Cantidad de la posición (positiva para long, negativa para short)
    public decimal EntryPrice { get; set; }
    public decimal MarkPrice { get; set; }
    public decimal UnrealizedProfit { get; set; }
    public string Side { get; set; } // LONG o SHORT
    public int Leverage { get; set; }
}
using System;

namespace Mexc.API.Models;

public class MexcSubmitOrderRequest
{
    public string Symbol { get; set; }
    public string Side { get; set; }
    public string Type { get; set; }
    public string Quantity { get; set; }
    public string Price { get; set; }
    public string StopPrice { get; set; }
    public bool ReduceOnly { get; set; }
}

public class MexcUpdateOrderRequest
{
    public string OrderId { get; set; }
    public string Symbol { get; set; }
    public string Side { get; set; }
    public string Quantity { get; set; }
    public string Price { get; set; }
    public string StopPrice { get; set; }
}
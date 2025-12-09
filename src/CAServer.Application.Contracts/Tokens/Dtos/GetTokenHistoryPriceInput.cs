using System;

namespace CAServer.Tokens.Dtos;

public class GetTokenHistoryPriceInput
{
    public string Symbol { get; set; }
    public DateTime DateTime { get; set; }
}
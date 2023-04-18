using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetTokenDto
{
    public List<Token> Tokens { get; set; }
}

public class Token
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string Balance { get; set; }
    public int Decimal { get; set; }
    public string BalanceInUsd { get; set; }
}
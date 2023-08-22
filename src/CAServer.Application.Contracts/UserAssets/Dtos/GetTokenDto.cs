using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetTokenDto
{
    public List<Token> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class Token
{
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public string Balance { get; set; }
    public int Decimals { get; set; }
    public string BalanceInUsd { get; set; }
    public string TokenContractAddress { get; set; }
    public string ImageUrl { get; set; }
    public long IssueChainId { get; set; }
    
    
}
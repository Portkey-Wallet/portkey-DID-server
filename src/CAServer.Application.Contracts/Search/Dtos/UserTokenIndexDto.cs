using System;

namespace CAServer.Search.Dtos;

public class UserTokenIndexDto
{
    public Guid UserId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsDefault { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
}

public class Token
{
    public Guid Id { get; set; }
    public string ChainId {get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string ImageUrl { get; set; }
}
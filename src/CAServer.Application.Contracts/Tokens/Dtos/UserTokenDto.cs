using System;
using Orleans;

namespace CAServer.Tokens.Dtos;

[GenerateSerializer]
public class UserTokenDto
{
    [Id(0)]
    public Guid Id { get; set; }
    [Id(1)]
    public Guid UserId { get; set; }
    [Id(2)]
    public bool IsDefault { get; set; }
    [Id(3)]
    public bool IsDisplay { get; set; }
    [Id(4)]
    public int SortWeight { get; set; }
    [Id(5)]
    public Token Token { get; set; }
}


public class Token
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}
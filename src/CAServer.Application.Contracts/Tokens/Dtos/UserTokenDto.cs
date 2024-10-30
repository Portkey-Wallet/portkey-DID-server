using System;
using CAServer.Commons.Etos;

namespace CAServer.Tokens.Dtos;

public class UserTokenDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
    public int SortWeight { get; set; }
    public Token Token { get; set; }
}

public class Token : ChainDisplayNameDto
{
    public Guid Id { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}
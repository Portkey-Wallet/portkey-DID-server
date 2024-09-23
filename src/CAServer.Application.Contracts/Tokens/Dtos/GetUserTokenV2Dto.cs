using System.Collections.Generic;

namespace CAServer.Tokens.Dtos;

public class GetUserTokenV2Dto
{
    public string Symbol { get; set; }
    public string ImageUrl { get; set; }
    public string DisplayStatus { get; set; }
    public string Label { get; set; }
    public bool IsDefault { get; set; }
    public List<GetUserTokenDto> Tokens { get; set; } = new();
}

public enum TokenDisplayStatus
{
    All,
    Partial,
    None
}
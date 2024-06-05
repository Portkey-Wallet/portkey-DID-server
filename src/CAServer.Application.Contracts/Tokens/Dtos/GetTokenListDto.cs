namespace CAServer.Tokens.Dtos;

public class GetTokenListDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public string TokenName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
    public string Label { get; set; }
}
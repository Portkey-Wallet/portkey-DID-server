namespace CAServer.Tokens.Dtos;

public class GetUserTokenDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
    public bool IsDefault { get; set; }
    public bool IsDisplay { get; set; }
}
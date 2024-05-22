namespace CAServer.Transfer.Dtos;

public class GetDepositRequestDto
{
    public string ChainId { get; set; }
    public string Network { get; set; }
    public string Symbol { get; set; }
    public string? ToSymbol { get; set; }
}
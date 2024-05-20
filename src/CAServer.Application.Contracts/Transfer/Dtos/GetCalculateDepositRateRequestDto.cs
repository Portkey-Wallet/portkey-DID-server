namespace CAServer.Transfer.Dtos;

public class GetCalculateDepositRateRequestDto
{
    public string ToChainId { get; set; }
    public string FromSymbol { get; set; }
    public string ToSymbol { get; set; }
    public decimal FromAmount { get; set; }
}
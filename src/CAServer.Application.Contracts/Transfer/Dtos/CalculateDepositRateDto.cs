namespace CAServer.Transfer.Dtos;

public class CalculateDepositRateDto
{
    public ConversionRate ConversionRate { get; set; }
}

public class ConversionRate
{
    public string FromSymbol { get; set; }
    public string ToSymbol { get; set; }
    public decimal FromAmount { get; set; }
    public decimal ToAmount { get; set; }
    public decimal MinimumReceiveAmount { get; set; }
}
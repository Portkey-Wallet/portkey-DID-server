namespace CAServer.BackGround.Dtos;

public class HandleTransactionDto
{
    public string ChainId { get; set; }
    public string RawTransaction { get; set; }
    public string MerchantName { get; set; }
    public Guid OrderId { get; set; }
}
namespace CAServer.RedPackage.Dtos;

public class GetCreationResultOutputDto
{
    public RedPackageTransactionStatus Status { get; set; }
    public string Message { get; set; } 
    public string TransactionResult { get; set; }
    public string TransactionId { get; set; }
}
namespace CAServer.ImTransfer.Dtos;

public class TransferResultDto
{
    public TransferTransactionStatus Status { get; set; }
    public string Message { get;set; }
    public string TransactionId { get;set; }
    public string TransactionResult { get;set; }
    public string BlockHash { get;set; }
    public string ChannelUuid { get;set; }
}
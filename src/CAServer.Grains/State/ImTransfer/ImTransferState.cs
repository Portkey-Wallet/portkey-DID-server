using CAServer.EnumType;

namespace CAServer.Grains.State.ImTransfer;

public class ImTransferState
{
    public string Id { get; set; }
    public long Amount { get; set; }
    public Guid SenderId { get; set; }
    public GroupType Type { get; set; }
    public Guid ToUserId { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public int Decimal { get; set; }
    public string Memo { get; set; }
    public string ChannelUuid { get; set; }
    public string RawTransaction { get; set; }
    public string Message { get; set; }
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public string TransactionResult { get; set; }
    public TransferTransactionStatus TransactionStatus { get; set; }
    public string ErrorMessage { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset ModificationTime { get; set; }
}
using System;
using CAServer.EnumType;

namespace CAServer.ImTransfer.Etos;

public class TransferEto
{
    public string Id { get; set; }
    public long Amount { get; set; }
    public Guid UserId { get; set; }
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
    public string TransactionResult { get; set; }
    public RedPackageTransactionStatus TransactionStatus { get; set; }
    public string ErrorMessage { get; set; }
    public DateTimeOffset CreateTime { get; set; }
    public DateTimeOffset ModificationTime { get; set; }
    public string SenderRelationToken { get; set; }
    public string SenderPortkeyToken { get; set; }
}
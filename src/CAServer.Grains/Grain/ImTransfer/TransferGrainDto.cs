using CAServer.EnumType;

namespace CAServer.Grains.Grain.ImTransfer;

[GenerateSerializer]
public class TransferGrainDto
{
    [Id(0)]
    public string Id { get; set; }

    [Id(1)]
    public long Amount { get; set; }

    [Id(2)]
    public Guid SenderId { get; set; }

    [Id(3)]
    public GroupType Type { get; set; }

    [Id(4)]
    public Guid ToUserId { get; set; }

    [Id(5)]
    public string ChainId { get; set; }

    [Id(6)]
    public string Symbol { get; set; }

    [Id(7)]
    public int Decimal { get; set; }

    [Id(8)]
    public string Memo { get; set; }

    [Id(9)]
    public string ChannelUuid { get; set; }

    [Id(10)]
    public string RawTransaction { get; set; }

    [Id(11)]
    public string Message { get; set; }

    [Id(12)]
    public string TransactionId { get; set; }

    [Id(13)]
    public string BlockHash { get; set; }

    [Id(14)]
    public string TransactionResult { get; set; }

    [Id(15)]
    public TransferTransactionStatus TransactionStatus { get; set; }

    [Id(16)]
    public string ErrorMessage { get; set; }

    [Id(17)]
    public DateTimeOffset CreateTime { get; set; }

    [Id(18)]
    public DateTimeOffset ModificationTime { get; set; }
}
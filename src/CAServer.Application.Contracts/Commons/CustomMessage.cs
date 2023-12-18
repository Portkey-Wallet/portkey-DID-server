using System;

namespace CAServer.Commons;

public class CustomMessage<T>
{
    public string Image { get; set; }
    public string Link { get; set; }
    public T Data { get; set; }
}

public class TransferCustomMessage<T> : CustomMessage<T>
{
    public TransferExtraData TransferExtraData { get; set; }
}

public class RedPackageCard
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public string Memo { get; set; }
}

public class TransferCard
{
    public string Id { get; set; }
    public Guid SenderId { get; set; }
    public string Memo { get; set; }
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
}

public class TransferExtraData
{
    public long Amount { get; set; }
    public int Decimal { get; set; }
    public string Symbol { get; set; }
}
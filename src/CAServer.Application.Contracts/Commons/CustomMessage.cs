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
    public string SenderId { get; set; }
    public string SenderName { get; set; }
    public string Memo { get; set; }
    public string TransactionId { get; set; }
    public string BlockHash { get; set; }
    public string ToUserId { get; set; }
    public string ToUserName { get; set; }
}

public class TransferExtraData
{
    public TransferTokenInfo TokenInfo { get; set; }
    public TransferNftInfo NftInfo { get; set; }
}

public class TransferTokenInfo
{
    public long Amount { get; set; }
    public int Decimal { get; set; }
    public string Symbol { get; set; }
}

public class TransferNftInfo
{
    public string NftId { get; set; }
    public string Alias { get; set; }
}
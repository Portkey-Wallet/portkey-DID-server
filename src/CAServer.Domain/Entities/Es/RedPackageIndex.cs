using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.EnumType;
using Nest;

namespace CAServer.Entities.Es;

public class RedPackageIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public RedPackageDisplayType RedPackageDisplayType { get; set; }
    public bool IsNewUsersOnly { get; set; }
    [Keyword] public Guid RedPackageId { get; set; }
    public long TotalAmount { get; set; }
    public long MinAmount { get; set; }
    public string Memo { get; set; } = string.Empty;
    [Keyword] public Guid SenderId { get; set; }
    [Keyword] public long CreateTime { get; set; }
    [Keyword] public long EndTime { get; set; }
    [Keyword] public long ExpireTime { get; set; }
    [Keyword] public string Symbol { get; set; }
    public int Decimal { get; set; }
    public int Count { get; set; }
    [Keyword] public string ChannelUuid { get; set; }
    public string SendUuid { get; set; }
    public string Message { get; set; }
    [Keyword] public RedPackageType Type { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string TransactionResult { get; set; }
    [Keyword] public string PayedTransactionIds { get; set; }
    public List<PayedTransactionDto> PayedTransactionDtoList { get; set; }
    [Keyword] public string RefundedTransactionId { get; set; }
    public string RefundedTransactionResult { get; set; }
    public RedPackageTransactionStatus RefundedTransactionStatus { get; set; }
    public string ErrorMessage { get; set; }
    public string SenderRelationToken { get; set; }
    public string SenderPortkeyToken { get; set; }
    public RedPackageTransactionStatus TransactionStatus { get; set; }
    
    public List<GrabItemDto> Items { get; set; }
    
    public int AssetType { get; set; }
    
    public class GrabItemDto
    {
        public string Amount { get; set; }
        public string CaAddress { get; set; }
        public Guid UserId { get; set; }
        public bool PaymentCompleted{ get; set; }
    }
    
    public class PayedTransactionDto
    {
        public string PayedTransactionId { get; set; }
        public string PayedTransactionResult { get; set; }
        public RedPackageTransactionStatus PayedTransactionStatus { get; set; }
    }
}
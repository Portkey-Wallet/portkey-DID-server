using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class NftOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string NftSymbol { get; set; }
    [Keyword] public string NftCollectionName { get; set; }
    [Keyword] public string MerchantName { get; set; }
    [Keyword] public string MerchantAddress { get; set; }
    [Keyword] public string MerchantOrderId { get; set; }
    [Keyword] public string CaHash { get; set; }
    public string NftPicture { get; set; }
    
    [Keyword] public string WebhookTime { get; set; }
    [Keyword] public string WebhookStatus { get; set; }
    public string WebhookUrl { get; set; }
    public string WebhookResult { get; set; }
    public int WebhookCount { get; set; } = 0;
    
    [Keyword] public string ThirdPartNotifyStatus { get; set; }
    [Keyword] public string ThirdPartNotifyTime { get; set; }
    public string ThirdPartNotifyResult { get; set; }
    public int ThirdPartNotifyCount { get; set; } = 0;
    
    public DateTime CreateTime { get; set; }
    public DateTime ExpireTime { get; set; }

}
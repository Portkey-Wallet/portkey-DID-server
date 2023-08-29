using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class NftOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public string NftSymbol { get; set; }
    [Keyword] public string MerchantName { get; set; }
    [Keyword] public string MerchantOrderId { get; set; }
    [Keyword] public string NftPicture { get; set; }
    [Keyword] public string WebhookUrl { get; set; }
    [Keyword] public string WebhookResult { get; set; }
    [Keyword] public string WebhookTime { get; set; }
    [Keyword] public string WebhookStatus { get; set; }
    public int WebhookCount { get; set; } = 0;
}
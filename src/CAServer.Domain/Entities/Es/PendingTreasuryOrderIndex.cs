using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using CAServer.ThirdPart;
using CAServer.Tokens;
using Nest;

namespace CAServer.Entities.Es;

public class PendingTreasuryOrderIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    
    [Keyword] public string ThirdPartName { get; set; }
    [Keyword] public string ThirdPartOrderId { get; set; }
    public long LastModifyTime { get; set; }
    public long ExpireTime { get; set; }
    public long CreateTime { get; set; }
    [Keyword] public string Status { get; set; }
    
    public TreasuryOrderRequest TreasuryOrderRequest { get; set; }
    public TokenExchange TokenExchange { get; set; }
    public List<FeeItem> FeeInfo { get; set; }
    
}
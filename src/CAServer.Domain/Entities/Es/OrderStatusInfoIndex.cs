using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class OrderStatusInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid OrderId { get; set; }
    [Keyword] public string ThirdPartOrderNo { get; set; }
    public List<OrderStatusInfo> OrderStatusList { get; set; }
}

public class OrderStatusInfo
{
    [Keyword] public string Status { get; set; }
    [Keyword] public long LastModifyTime { get; set; }
    [Keyword] public string Extension { get; set; }
}
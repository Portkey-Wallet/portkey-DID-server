using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class ChainsInfoIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string ChainName { get; set; }
    [Keyword] public string EndPoint { get; set; }
    [Keyword] public string ExplorerUrl { get; set; }
    [Keyword] public string CaContractAddress { get; set; }
    public DateTime LastModifyTime { get; set; }
}
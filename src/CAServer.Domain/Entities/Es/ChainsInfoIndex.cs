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
    public DefaultTokenInfo DefaultToken { get; set; }
    public DateTime LastModifyTime { get; set; }
}

public class DefaultTokenInfo
{
    [Keyword] public string Name { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string ImageUrl { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string Decimals { get; set; }
}
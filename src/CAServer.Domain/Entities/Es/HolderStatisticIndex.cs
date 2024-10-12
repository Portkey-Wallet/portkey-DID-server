using System;
using AElf.Indexing.Elasticsearch;
using CAServer.Account;
using Nest;

namespace CAServer.Entities.Es;

public class HolderStatisticIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaHash { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string IpAddress { get; set; }
    public CountryInfo CountryInfo { get; set; }
    [Keyword] public string ActivityId { get; set; }
    [Keyword] public string Status { get; set; }
    [Keyword] public string OperationType { get; set; }
    [Keyword] public DateTime CreateTime { get; set; }
}

public class CountryInfo
{
    [Keyword] public string CountryCode { get; set; }
    [Keyword] public string CountryName { get; set; }
    [Keyword] public string RegionCode { get; set; }
    [Keyword] public string RegionName { get; set; }
    [Keyword] public string City { get; set; }
}
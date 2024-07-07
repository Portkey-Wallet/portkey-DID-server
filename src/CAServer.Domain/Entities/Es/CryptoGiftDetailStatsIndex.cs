using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class CryptoGiftDetailStatsIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public bool IsNewUsersOnly { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public int Number { get; set; }
    [Keyword] public int Count { get; set; }
    [Keyword] public int Grabbed { get; set; }
    public string Symbols { get; set; }
    public long CreateTime { get; set; }
}
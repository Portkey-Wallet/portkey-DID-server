using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class CryptoGiftNumStatsIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public bool IsNewUsersOnly { get; set; }
    [Keyword] public string Date { get; set; }
    [Keyword] public int Number { get; set; }
    public string Symbols { get; set; }
    public long CreateTime { get; set; }
}
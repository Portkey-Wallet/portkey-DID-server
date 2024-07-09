using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class CryptoGiftNewUsersOnlyNumStatsIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public string Date { get; set; }
    [Keyword] public int Number { get; set; }
    public string Symbols { get; set; }
    public long CreateTime { get; set; }
}
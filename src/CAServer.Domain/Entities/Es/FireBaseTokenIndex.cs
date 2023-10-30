using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class FireBaseTokenIndex : CAServerEsEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public Guid PortKeyId { get; set; }
    [Keyword] public string Token { get; set; }
    [Keyword] public string DeviceId { get; set; }
    [Keyword] public string AppStatus { get; set; }
    public long RefreshTime { get; set; }
    public DateTime ModificationTime { get; set; }
}

public enum AppStatus
{
    Foreground,
    Background,
    Offline
}
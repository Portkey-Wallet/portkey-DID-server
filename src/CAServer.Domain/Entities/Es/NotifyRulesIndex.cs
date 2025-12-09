using System;
using AElf.Indexing.Elasticsearch;
using Nest;

namespace CAServer.Entities.Es;

public class NotifyRulesIndex : CAServerEsEntity<Guid>, IIndexBuild
{
    [Keyword] public override Guid Id { get; set; }
    public int NotifyId { get; set; }
    [Keyword] public string AppId { get; set; }
    [Keyword] public string[] AppVersions { get; set; }
    [Keyword] public string[] DeviceTypes { get; set; }
    [Keyword] public string[] DeviceBrands { get; set; }
    [Keyword] public string[] OperatingSystemVersions { get; set; }
    public NotifySendType[] SendTypes { get; set; }
    [Keyword] public string[] Countries { get; set; }
    public bool IsApproved { get; set; }
}
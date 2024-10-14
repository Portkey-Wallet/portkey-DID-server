using System;
using Orleans;

namespace CAServer.Upgrade.Dtos;

[GenerateSerializer]
public class UpgradeBaseDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public Guid UserId { get; set; }
    [Id(2)]
    public DateTime CreateTime { get; set; }
    [Id(3)]
    public bool IsPopup { get; set; }
    [Id(4)]
    public string Version { get; set; }
}
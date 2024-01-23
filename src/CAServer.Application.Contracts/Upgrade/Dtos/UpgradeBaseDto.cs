using System;

namespace CAServer.Upgrade.Dtos;

public class UpgradeBaseDto
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsPopup { get; set; }
    public string Version { get; set; }
}
namespace CAServer.Grains.State.Upgrade;

public class UpgradeState
{
    public string Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreateTime { get; set; }
    public bool IsPopup { get; set; }
    public string Version { get; set; }
}
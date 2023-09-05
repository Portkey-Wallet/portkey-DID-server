namespace CAServer.Grains.State;

public class CAHolderState
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CaHash { get; set; }
    public string Nickname { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreateTime { get; set; }
}
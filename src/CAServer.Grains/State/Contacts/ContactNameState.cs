namespace CAServer.Grains.State.Contacts;

public class ContactNameState
{
    public string ContactName { get; set; }
    public bool IsDeleted { get; set; } = true;
    public Guid UserId { get; set; }
}
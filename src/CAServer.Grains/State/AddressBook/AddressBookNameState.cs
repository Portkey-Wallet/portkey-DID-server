namespace CAServer.Grains.State.AddressBook;

public class AddressBookNameState
{
    public string ContactName { get; set; }
    public bool IsDeleted { get; set; } = true;
    public Guid UserId { get; set; }
}
namespace CAServer.Grains.State.Contacts;

[GenerateSerializer]
public class ContactNameState
{
	[Id(0)]
    public string ContactName { get; set; }
	[Id(1)]
    public bool IsDeleted { get; set; } = true;
	[Id(2)]
    public Guid UserId { get; set; }
}

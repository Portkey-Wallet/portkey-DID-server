namespace CAServer.Grains.State;

[GenerateSerializer]
public class CAHolderState
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public Guid UserId { get; set; }
	[Id(2)]
    public string CaHash { get; set; }
	[Id(3)]
    public string Nickname { get; set; }
	[Id(4)]
    public string Avatar { get; set; }
	[Id(5)]
    public bool IsDeleted { get; set; }
	[Id(6)]
    public DateTime CreateTime { get; set; }
    
	[Id(7)]
    public bool PopedUp { get; set; }
    
	[Id(8)]
    public bool ModifiedNickname { get; set; }

	[Id(9)]
    public string IdentifierHash { get; set; }
    
	[Id(10)]
    public string SecondaryEmail { get; set; }
}

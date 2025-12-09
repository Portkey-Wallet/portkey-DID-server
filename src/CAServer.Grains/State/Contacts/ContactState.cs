using CAServer.Contacts;
using CAServer.Grains.Grain.Contacts;

namespace CAServer.Grains.State.Contacts;

[GenerateSerializer]
public class ContactState
{
	[Id(0)]
    public Guid Id { get; set; }
	[Id(1)]
    public string Index { get; set; }
	[Id(2)]
    public string Name { get; set; }
	[Id(3)]
    public string Avatar { get; set; }
	[Id(4)]
    public List<ContactAddress> Addresses { get; set; } = new();
	[Id(5)]
    public Guid UserId { get; set; }
	[Id(6)]
    public CaHolderInfo CaHolderInfo { get; set; }
	[Id(7)]
    public ImInfo ImInfo { get; set; }
	[Id(8)]
    public bool IsDeleted { get; set; } = true;
	[Id(9)]
    public bool IsImputation { get; set; }
	[Id(10)]
    public DateTime CreateTime { get; set; }
	[Id(11)]
    public DateTime ModificationTime { get; set; }

	[Id(12)]
    public int ContactType { get; set; }
}

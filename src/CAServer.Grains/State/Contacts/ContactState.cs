using CAServer.Etos;
using CAServer.Grains.Grain.Contacts;

namespace CAServer.Grains.State.Contacts;

public class ContactState
{
    public Guid Id { get; set; }
    public string Index { get; set; }
    public string Name { get; set; }
    public List<ContactAddress> Addresses { get; set; } = new();
    public Guid UserId { get; set; }
    public bool IsDeleted { get; set; } = true;
    public DateTime ModificationTime { get; set; }
}
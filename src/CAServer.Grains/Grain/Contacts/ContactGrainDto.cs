using CAServer.Contacts;

namespace CAServer.Grains.Grain.Contacts;

public class ContactGrainDto : ContactDto
{
    public int ContactType { get; set; }
}
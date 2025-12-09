using CAServer.Contacts;

namespace CAServer.Grains.Grain.Contacts;

[GenerateSerializer]
public class ContactGrainDto : ContactDto
{
    [Id(0)]
    public int ContactType { get; set; }
}
using System.Collections.Generic;

namespace CAServer.Contacts;

public class ContactListRequestDto
{
    public List<string> ContactUserIds { get; set; }
    public string Address { get; set; }
}
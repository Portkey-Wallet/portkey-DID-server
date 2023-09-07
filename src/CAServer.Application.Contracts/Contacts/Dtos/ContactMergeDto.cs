using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class ContactMergeDto
{
    public ImInfo ImInfo { get; set; }
    [Required] public List<ContactAddressDto> Addresses { get; set; }
}
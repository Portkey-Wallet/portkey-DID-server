using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class ContactMergeDto
{
    [Required] public List<ContactAddressDto> Addresses { get; set; }
}
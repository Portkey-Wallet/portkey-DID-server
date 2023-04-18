using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CAServer.Contacts;

public class CreateUpdateContactDto
{
    [Required]
    [RegularExpression(@"^[a-zA-Z\d'_'' '\s]{1,16}$")]
    public string Name { get; set; }

    [ValidAddresses] public List<ContactAddressDto> Addresses { get; set; }
}
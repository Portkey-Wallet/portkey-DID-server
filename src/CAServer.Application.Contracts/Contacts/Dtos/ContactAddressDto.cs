using System.ComponentModel.DataAnnotations;

namespace CAServer.Contacts;

public class ContactAddressDto
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public string Address { get; set; }
}
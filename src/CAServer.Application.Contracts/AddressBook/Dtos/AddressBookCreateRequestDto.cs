using System.ComponentModel.DataAnnotations;

namespace CAServer.AddressBook.Dtos;

public class AddressBookCreateRequestDto
{
    [RegularExpression(@"^[a-zA-Z\d'_'' '\s]{1,16}$")]
    [Required]
    public string Name { get; set; }

    [Required] public string Address { get; set; }
    [Required] public string Network { get; set; }
    public string ChainId { get; set; }
    public bool IsExchange { get; set; }
}
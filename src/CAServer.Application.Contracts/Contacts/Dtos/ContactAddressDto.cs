using System.ComponentModel.DataAnnotations;
using CAServer.Commons;

namespace CAServer.Contacts;

public class ContactAddressDto
{
    [Required] public string ChainId { get; set; }

    public string ChainName { get; set; } = CommonConstant.ChainName;
    [Required] public string Address { get; set; }
}
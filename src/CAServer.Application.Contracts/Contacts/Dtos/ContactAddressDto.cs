using System.ComponentModel.DataAnnotations;
using CAServer.Commons;
using Orleans;

namespace CAServer.Contacts;

[GenerateSerializer]
public class ContactAddressDto
{
    [Id(0)]
    [Required] public string ChainId { get; set; }

    [Id(1)]
    public string ChainName { get; set; } = CommonConstant.ChainName;
    
    [Id(2)]
    [Required] public string Address { get; set; }
    
    [Id(3)]
    public string Image { get; set; }
}
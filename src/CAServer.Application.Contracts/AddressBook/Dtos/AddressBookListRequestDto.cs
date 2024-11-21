using Volo.Abp.Application.Dtos;

namespace CAServer.AddressBook.Dtos;

public class AddressBookListRequestDto : PagedResultRequestDto
{
    public string Filter { get; set; }
    public string Sort { get; set; }
}
using System;
using System.Threading.Tasks;
using CAServer.AddressBook.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.AddressBook;

public interface IAddressBookAppService
{
    Task<AddressBookDto> CreateAsync(AddressBookCreateRequestDto requestDto);
    Task<AddressBookDto> UpdateAsync(AddressBookUpdateRequestDto requestDto);
    Task DeleteAsync(AddressBookDeleteRequestDto requestDto);
    Task<AddressBookExistDto> ExistAsync(string name);
    Task<PagedResultDto<AddressBookDto>> GetListAsync(AddressBookListRequestDto requestDto);
    Task<GetNetworkListDto> GetNetworkListAsync();
    Task<AddressBookDto> MigrateAsync(Guid contactId);
}
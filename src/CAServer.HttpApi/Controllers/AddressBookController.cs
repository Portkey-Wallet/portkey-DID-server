using System;
using System.Threading.Tasks;
using CAServer.AddressBook;
using CAServer.AddressBook.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("AddressBook")]
[Route("api/app/address-book")]
[Authorize]
[IgnoreAntiforgeryToken]
public class AddressBookController : CAServerController
{
    private readonly IAddressBookAppService _addressBookAppService;

    public AddressBookController(IAddressBookAppService addressBookAppService)
    {
        _addressBookAppService = addressBookAppService;
    }

    [HttpPost("create")]
    public async Task<AddressBookDto> CreateAsync(AddressBookCreateRequestDto requestDto)
    {
        return await _addressBookAppService.CreateAsync(requestDto);
    }

    [HttpPost("update")]
    public async Task<AddressBookDto> UpdateAsync(AddressBookUpdateRequestDto requestDto)
    {
        return await _addressBookAppService.UpdateAsync(requestDto);
    }

    [HttpPost("delete")]
    public async Task DeleteAsync(AddressBookDeleteRequestDto requestDto)
    {
        await _addressBookAppService.DeleteAsync(requestDto);
    }

    [HttpGet("exist")]
    public async Task<AddressBookExistDto> ExistAsync(string name)
    {
        return await _addressBookAppService.ExistAsync(name);
    }

    [HttpGet("list")]
    public async Task<PagedResultDto<AddressBookDto>> GetListAsync(AddressBookListRequestDto requestDto)
    {
        return await _addressBookAppService.GetListAsync(requestDto);
    }

    [HttpGet("network"), AllowAnonymous]
    public async Task<GetNetworkListDto> GetNetworkListAsync()
    {
        return await _addressBookAppService.GetNetworkListAsync();
    }

    [HttpPost("migrate"), AllowAnonymous]
    public async Task<AddressBookDto> MigrateAsync([FromForm] Guid contactId)
    {
        return await _addressBookAppService.MigrateAsync(contactId);
    }
}
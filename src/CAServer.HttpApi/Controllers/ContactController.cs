using System;
using System.Threading.Tasks;
using CAServer.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contact")]
[Route("api/app/contacts")]
[Authorize]
public class ContactController : CAServerController
{
    private readonly IContactAppService _contactAppService;

    public ContactController(IContactAppService contactAppService)
    {
        _contactAppService = contactAppService;
    }

    [HttpPost]
    public async Task<ContactResultDto> CreateAsync(CreateUpdateContactDto input)
    {
        return await _contactAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public async Task<ContactResultDto> UpdateAsync(Guid id, CreateUpdateContactDto input)
    {
        return await _contactAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public async Task DeleteAsync(Guid id)
    {
        await _contactAppService.DeleteAsync(id);
    }

    [HttpGet("exist")]
    public async Task<ContractExistDto> GetExistAsync(string name)
    {
        return await _contactAppService.GetExistAsync(name);
    }
}
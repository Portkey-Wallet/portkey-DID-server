using System;
using System.Threading.Tasks;
using CAServer.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contact")]
[Route("api/app/contacts")]
[IgnoreAntiforgeryToken]
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

    [HttpGet("{id}")]
    public async Task<ContactResultDto> GetAsync(Guid id)
    {
        return await _contactAppService.GetAsync(id);
    }

    [HttpGet("list")]
    public async Task<PagedResultDto<ContactResultDto>> GetListAsync(ContactGetListDto input)
    {
        return await _contactAppService.GetListAsync(input);
    }

    [HttpPost("merge")]
    public async Task MergeAsync(ContactMergeDto input)
    {
        throw new NotImplementedException();
        // return await _contactAppService.GetExistAsync(name);
    }

    [HttpGet("isImputation")]
    public async Task<ContactImputationDto> GetImputationAsync()
    {
        return await _contactAppService.GetImputationAsync();
    }

    [HttpPost("read")]
    public async Task ReadImputationAsync()
    {
        await _contactAppService.ReadImputationAsync();
    }

    [HttpGet("getContact")]
    public async Task<ContactResultDto> GetContactAsync(Guid contactUserId)
    {
        return await _contactAppService.GetContactAsync(contactUserId);
    }
}
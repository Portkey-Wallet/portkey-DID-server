using System;
using System.Collections.Generic;
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
    public async Task<PagedResultDto<ContactListDto>> GetListAsync(ContactGetListDto input)
    {
        return await _contactAppService.GetListAsync(input);
    }

    [HttpGet("isImputation")]
    public async Task<ContactImputationDto> GetImputationAsync()
    {
        return await _contactAppService.GetImputationAsync();
    }

    [HttpPost("read")]
    public async Task ReadImputationAsync(ReadImputationDto input)
    {
        await _contactAppService.ReadImputationAsync(input);
    }

    [HttpGet("getContact")]
    public async Task<ContactResultDto> GetContactAsync(Guid contactUserId)
    {
        return await _contactAppService.GetContactAsync(contactUserId);
    }
    
    [HttpPost("getContactList")]
    public async Task<List<ContactResultDto>> GetContactListAsync(ContactListRequestDto input)
    {
        return await _contactAppService.GetContactListAsync(input);
    }
    
    [HttpGet("getContactsByUserId")]
    public async Task<List<ContactResultDto>> GetContactsByUserIdAsync(Guid userId)
    {
        return await _contactAppService.GetContactsByUserIdAsync(userId);
    }
    
    [HttpPost("invitationPermission")]
    public async Task<List<ContactResultDto>> ContactsInvitationPermission(ContactLabelDto contactLabelDto)
    {
        var userId = CurrentUser.Id;
        if (null == userId)
        {
            throw new UserFriendlyException("Invalidate User");
        }

        return await _contactAppService.InvitationPermission(userId,contactLabelDto);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Contacts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver.Linq;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using ILogger = Google.Apis.Logging.ILogger;

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
    private readonly ILogger<ContactController> _logger;

    public ContactController(IContactAppService contactAppService, ILogger<ContactController> logger)
    {
        _contactAppService = contactAppService;
        _logger = logger;
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
        var result = await _contactAppService.GetListAsync(input);
        var headers = Request.Headers;
        var platform = headers["platform"];
        var version = headers["version"];
        _logger.LogDebug("platform is {platform},version is {version}",platform,version);
        if (string.IsNullOrEmpty(platform) && string.IsNullOrEmpty(version))
        {
            var curVersion = new Version(version.ToString().Replace("v",""));
            var preVersion = new Version("v1.20.00".Replace("v",""));
            if (platform == "app" && curVersion >= preVersion)
            {
                return result;
            }
        }

        var contactListDtos = result.Items.Where(t=>t.ImInfo.RelationId != "jkhct-2aaaa-aaaaa-aaczq-cai").ToList();
        result.Items = contactListDtos;
        return result;
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

    [HttpPost("getContactsByRelationId")]
    public async Task<ContactResultDto> GetContactsByRelationIdAsync(ContactProfileRequestDto dto)
    {
        return await _contactAppService.GetContactsByRelationIdAsync(dto.UserId, dto.RelationId);
    }

    [HttpPost("getContactsByPortkeyId")]
    public async Task<ContactResultDto> GetContactsByPortkeyId(Guid userId, Guid portKeyId)
    {
        return await _contactAppService.GetContactsByPortkeyIdAsync(userId, portKeyId);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
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
    private readonly ChatBotOptions _botOptions;

    public ContactController(IContactAppService contactAppService, ILogger<ContactController> logger,
        IOptionsSnapshot<ChatBotOptions> botOptions)
    {
        _contactAppService = contactAppService;
        _logger = logger;
        _botOptions = botOptions.Value;
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
        foreach (var item in result.Items)
        {
            _logger.LogDebug("item is {item}", JsonConvert.SerializeObject(item));
        }

        var headers = Request.Headers;
        var platform = headers["platform"];
        var version = headers["version"];
        _logger.LogDebug("platform is {platform},version is {version}", platform, version);
        if (!string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(version))
        {
            var curVersion = new Version(version.ToString().Replace("v", ""));
            var preVersion = new Version(_botOptions.Version.Replace("v", ""));
            if (platform != "extension" && curVersion >= preVersion)
            {
                var contacts = result.Items.Where(t => !_botOptions.RelationId.Equals(t?.ImInfo?.RelationId)).ToList();
                result.Items = contacts;
                result.TotalCount = contacts.Count;
                return result;
            }
        }

        var contactListDtos = result.Items.Where(t => !_botOptions.RelationId.Equals(t?.ImInfo?.RelationId)).ToList();
        result.Items = contactListDtos;
        result.TotalCount = contactListDtos.Count;
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
        var result = await _contactAppService.GetContactListAsync(input);
        result = result.Where(t => !_botOptions.RelationId.Equals(t?.ImInfo?.RelationId)).ToList();
        return result;
    }

    [HttpGet("getContactsByUserId")]
    public async Task<List<ContactResultDto>> GetContactsByUserIdAsync(Guid userId)
    {
        var result = await _contactAppService.GetContactsByUserIdAsync(userId);
        result = result.Where(t => !_botOptions.RelationId.Equals(t?.ImInfo?.RelationId)).ToList();
        return result;
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
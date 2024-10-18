using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.PrivacyPermission;
using CAServer.PrivacyPermission.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("PrivacyPermission")]
[Route("api/app/privacyPermission")]
[Authorize]
public class PrivacyPermissionController
{
    private readonly IPrivacyPermissionAppService _privacyPermissionAppService;
    
    public PrivacyPermissionController(IPrivacyPermissionAppService privacyPermissionAppService)
    {
        _privacyPermissionAppService = privacyPermissionAppService;
    }
    
    [HttpGet]
    [Route("{id}")]
    public async Task<GetPrivacyPermissionAsyncResponseDto> GetPrivacyPermissionByIdAsync(Guid id)
    {
        var privacyPermissionDto = await _privacyPermissionAppService.GetPrivacyPermissionAsync(id);
        var permissions = new List<PermissionSetting>();
        permissions.AddRange(privacyPermissionDto.PhoneList);
        permissions.AddRange(privacyPermissionDto.EmailList);
        permissions.AddRange(privacyPermissionDto.AppleList);
        permissions.AddRange(privacyPermissionDto.GoogleList);
        //check permissions's detail
        permissions = await _privacyPermissionAppService.CheckPrivacyPermissionByIdAsync(permissions, id);

        return new GetPrivacyPermissionAsyncResponseDto
        {
            Permissions = permissions,
            UserId = privacyPermissionDto.UserId,
            Id = privacyPermissionDto.Id
        };
    }
    
    [HttpGet]
    public async Task<GetPrivacyPermissionAsyncResponseDto> GetPrivacyPermissionAsync()
    {
        var  privacyPermissionDto = await _privacyPermissionAppService.GetPrivacyPermissionAsync(Guid.Empty);
        var permissions = new List<PermissionSetting>();
        permissions.AddRange(privacyPermissionDto.PhoneList);
        permissions.AddRange(privacyPermissionDto.EmailList);
        permissions.AddRange(privacyPermissionDto.AppleList);
        permissions.AddRange(privacyPermissionDto.GoogleList);
        return new GetPrivacyPermissionAsyncResponseDto
        {
            Permissions =  permissions,
            UserId = privacyPermissionDto.UserId,
            Id = privacyPermissionDto.Id
        };
    }

    [HttpPost]
    public async Task SetPrivacyPermissionAsync(SetPrivacyPermissionInput input)
    {
        await _privacyPermissionAppService.SetPrivacyPermissionAsync(input);
    }
}
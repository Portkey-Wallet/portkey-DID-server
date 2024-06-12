using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using CAServer.CryptoGift;
using CAServer.CryptoGift.Dtos;
using CAServer.EnumType;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Users;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CryptoGift")]
[Route("api/app/cryptogift/")]
[IgnoreAntiforgeryToken]
public class CryptoGiftController : CAServerController
{
    private readonly ICryptoGiftAppService _cryptoGiftAppService;
    private readonly ILogger<CryptoGiftController> _logger;

    public CryptoGiftController(ICryptoGiftAppService cryptoGiftAppService,
        ILogger<CryptoGiftController> logger)
    {
        _cryptoGiftAppService = cryptoGiftAppService;
        _logger = logger;
    }
    
    [HttpGet("history/fist")]
    [Authorize]
    public async Task<CryptoGiftHistoryItemDto> GetFirstCryptoGiftHistoryDetailAsync()
    {
        var senderId = CurrentUser.GetId();
        if (Guid.Empty.Equals(senderId))
        {
            throw new UserFriendlyException("current sender user not exist!");
        }
        return await _cryptoGiftAppService.GetFirstCryptoGiftHistoryDetailAsync(senderId);
    }
    
    [HttpGet("histories")]
    [Authorize]
    public async Task<List<CryptoGiftHistoryItemDto>> ListCryptoGiftHistoriesAsync()
    {
        var senderId = CurrentUser.GetId();
        if (Guid.Empty.Equals(senderId))
        {
            throw new UserFriendlyException("current sender user not exist!");
        }
        return await _cryptoGiftAppService.ListCryptoGiftHistoriesAsync(senderId);
    }

    [HttpPost("grab")]
    public async Task<string> PreGrabCryptoGift([FromBody] PreGrabCryptoGiftCmd preGrabCryptoGiftCmd)
    {
        return await _cryptoGiftAppService.PreGrabCryptoGift(preGrabCryptoGiftCmd.Id);
    }
    
    [HttpGet("detail")]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync([Required] Guid id)
    {
        return await _cryptoGiftAppService.GetCryptoGiftDetailAsync(id);
    }
    
    [HttpGet("login/detail")]
    [Authorize]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync([Required] Guid id)
    {
        var receiverId = CurrentUser.GetId();
        if (Guid.Empty.Equals(receiverId))
        {
            throw new UserFriendlyException("current user not exist!");
        }
        return await _cryptoGiftAppService.GetCryptoGiftLoginDetailAsync(receiverId, id);
    }
}
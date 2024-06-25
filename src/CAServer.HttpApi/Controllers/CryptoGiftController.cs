using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.CryptoGift;
using CAServer.CryptoGift.Dtos;
using CAServer.RedPackage.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    
    [HttpGet("history/first")]
    [Authorize]
    public async Task<CryptoGiftHistoryItemDto> GetFirstCryptoGiftHistoryDetailAsync()
    {
        var senderId = CurrentUser.GetId();
        if (Guid.Empty.Equals(senderId))
        {
            throw new UserFriendlyException("current sender user not exist!");
        }
        _logger.LogInformation("sender:{0} is querying first crypto gift", senderId);
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
    public async Task<CryptoGiftIdentityCodeDto> PreGrabCryptoGift([FromBody] PreGrabCryptoGiftCmd preGrabCryptoGiftCmd)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var result = await _cryptoGiftAppService.PreGrabCryptoGift(preGrabCryptoGiftCmd.Id);
        sw.Stop();
        _logger.LogInformation($"statistics id:{preGrabCryptoGiftCmd.Id} PreGrabCryptoGift cost:{sw.ElapsedMilliseconds}ms");
        return result;
    }
    
    [HttpGet("detail")]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync([Required] Guid id)
    {
        return await _cryptoGiftAppService.GetCryptoGiftDetailAsync(id);
    }
    
    [HttpGet("login/detail")]
    // [Authorize]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync([Required] Guid id, [Required]string caHash)
    {
        // var receiverId = CurrentUser.GetId();
        // if (Guid.Empty.Equals(receiverId))
        // {
        //     throw new UserFriendlyException("current user not exist!");
        // }
        return  await _cryptoGiftAppService.GetCryptoGiftLoginDetailAsync(caHash, id);
    }

    [HttpGet("test/detail")]
    public async Task<CryptoGiftAppDto> GetCryptoGiftDetailFromGrainAsync(Guid redPackageId)
    {
        return await _cryptoGiftAppService.GetCryptoGiftDetailFromGrainAsync(redPackageId);
    }

    [HttpGet("test/items")]
    public async Task<PreGrabbedDto> ListCryptoPreGiftGrabbedItems(Guid redPackageId)
    {
        return await _cryptoGiftAppService.ListCryptoPreGiftGrabbedItems(redPackageId);
    }
}
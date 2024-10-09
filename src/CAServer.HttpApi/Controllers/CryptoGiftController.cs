using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using CAServer.CryptoGift;
using CAServer.CryptoGift.Dtos;
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
        var sw = new Stopwatch();
        sw.Start();
        var result = await _cryptoGiftAppService.PreGrabCryptoGift(preGrabCryptoGiftCmd.Id, preGrabCryptoGiftCmd.Random);
        sw.Stop();
        _logger.LogInformation($"statistics id:{preGrabCryptoGiftCmd.Id} PreGrabCryptoGift cost:{sw.ElapsedMilliseconds}ms");
        return result;
    }
    
    [HttpGet("detail")]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync([Required] Guid id, string random)
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var result = await _cryptoGiftAppService.GetCryptoGiftDetailAsync(id, random);
        sw.Stop();
        _logger.LogInformation($"statistics id:{id} GetCryptoGiftDetail cost:{sw.ElapsedMilliseconds}ms");
        return result;
    }
    
    [HttpGet("login/detail")]
    // [Authorize]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftLoginDetailAsync([Required] Guid id, [Required]string caHash, string random)
    {
        // var receiverId = CurrentUser.GetId();
        // if (Guid.Empty.Equals(receiverId))
        // {
        //     throw new UserFriendlyException("current user not exist!");
        // }
        return  await _cryptoGiftAppService.GetCryptoGiftLoginDetailAsync(caHash, id, random);
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

    [HttpPost("nums")]
    public async Task<List<CryptoGiftSentNumberDto>> ComputeCryptoGiftNumber([FromBody] CryptoGiftStatisticsRequestDto requestDto)
    {
        return await _cryptoGiftAppService.ComputeCryptoGiftNumber(requestDto.NewUsersOnly, requestDto.Symbols.ToArray(), requestDto.CreateTime);
    }
    
    [HttpPost("claim/stats")]
    public async Task<List<CryptoGiftClaimDto>> ComputeCryptoGiftClaimStatistics([FromBody] CryptoGiftStatisticsRequestDto requestDto)
    {
        return await _cryptoGiftAppService.ComputeCryptoGiftClaimStatistics(requestDto.NewUsersOnly, requestDto.Symbols.ToArray(), requestDto.CreateTime);
    }
}
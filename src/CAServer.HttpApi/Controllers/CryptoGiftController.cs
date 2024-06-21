using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CryptoGiftController(ICryptoGiftAppService cryptoGiftAppService,
        ILogger<CryptoGiftController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _cryptoGiftAppService = cryptoGiftAppService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
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
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var result = await _cryptoGiftAppService.GetCryptoGiftDetailAsync(id);
        sw.Stop();
        _logger.LogInformation($"statistics id:{id} GetCryptoGiftDetailAsync cost:{sw.ElapsedMilliseconds}ms");
        return result;
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
        Stopwatch sw = new Stopwatch();
        sw.Start();
        var result = await _cryptoGiftAppService.GetCryptoGiftLoginDetailAsync(caHash, id);
        sw.Stop();
        _logger.LogInformation($"statistics id:{id} GetCryptoGiftLoginDetailAsync cost:{sw.ElapsedMilliseconds}ms");
        return result;
    }

    [HttpGet("test/transfer")]
    public async Task TestCryptoGiftTransferToRedPackage(string caHash, string caAddress,
        Guid id, string identityCode, bool isNewUser)
    {
        await _cryptoGiftAppService.TestCryptoGiftTransferToRedPackage(caHash, caAddress,
            id, identityCode, isNewUser);
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
    
    [HttpGet("ip")]
    public string GetRemoteIp()
    {
        var clientIp = _httpContextAccessor?.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return clientIp.Split(',')[0].Trim();
        }

        var remoteIpAddress = _httpContextAccessor?.HttpContext?.Request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return string.Empty;
        }

        return remoteIpAddress.IsIPv4MappedToIPv6
            ? remoteIpAddress.MapToIPv4().ToString()
            : remoteIpAddress.MapToIPv6().ToString();
    }
    
    [HttpGet("ip/async")]
    public async Task<string> GetRemoteIpAsync()
    {
        var clientIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return clientIp.Split(',')[0].Trim();
        }

        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return string.Empty;
        }
        return remoteIpAddress.IsIPv4MappedToIPv6 ? remoteIpAddress.MapToIPv4().ToString() : remoteIpAddress.MapToIPv6().ToString();
    }
}
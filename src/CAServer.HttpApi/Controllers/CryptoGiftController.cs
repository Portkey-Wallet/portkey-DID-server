using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
        return await _cryptoGiftAppService.PreGrabCryptoGift(preGrabCryptoGiftCmd.Id);
    }
    
    [HttpGet("detail")]
    public async Task<CryptoGiftPhaseDto> GetCryptoGiftDetailAsync([Required] Guid id, string ipAddress)
    {
        return await _cryptoGiftAppService.GetCryptoGiftDetailAsync(id, ipAddress);
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
        return await _cryptoGiftAppService.GetCryptoGiftLoginDetailAsync(caHash, id);
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
        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        _logger.LogInformation("IsIPv6Multicast:{0},IsIPv6Teredo:{1},IsIPv6LinkLocal:{2}," +
                               "IsIPv6SiteLocal:{3},IsIPv6UniqueLocal:{4},IsIPv4MappedToIPv6:{5}",
            remoteIpAddress.IsIPv6Multicast,
            remoteIpAddress.IsIPv6Teredo,
            remoteIpAddress.IsIPv6LinkLocal,
            remoteIpAddress.IsIPv6SiteLocal,
            remoteIpAddress.IsIPv6UniqueLocal,
            remoteIpAddress.IsIPv4MappedToIPv6);
        _logger.LogInformation("MapToIPv4:{0}", remoteIpAddress.MapToIPv4().ToString());
        _logger.LogInformation("MapToIPv6:{0}", remoteIpAddress.MapToIPv6().ToString());
        string ipAddress = remoteIpAddress?.ToString();
        return ipAddress;
    }
    
    [HttpGet("ip/async")]
    public async Task<string> GetRemoteIpAsync()
    {
        var clientIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(clientIp))
        {
            return clientIp;
        }

        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        if (remoteIpAddress == null)
        {
            return string.Empty;
        }
        return remoteIpAddress.IsIPv4MappedToIPv6 ? remoteIpAddress.MapToIPv4().ToString() : remoteIpAddress.MapToIPv6().ToString();
    }
}
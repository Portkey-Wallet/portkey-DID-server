using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAActivity;
using CAServer.CAActivity.Dto;
using CAServer.CAActivity.Dtos;
using CAServer.CAActivity.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("UserActivity")]
[Route("api/app/user/activities")]
[Authorize]
public class UserActivityController
{
    private readonly IUserActivityAppService _userActivityAppService;

    public UserActivityController(IUserActivityAppService userActivityAppService)
    {
        _userActivityAppService = userActivityAppService;
    }

    [HttpPost("transactions")]
    public async Task<GetActivitiesDto> GetTransactionsAsync(GetTwoCaTransactionRequestDto requestDto)
    {
        return await _userActivityAppService.GetTwoCaTransactionsAsync(requestDto);
    }

    [HttpPost("activities")]
    public async Task<GetActivitiesDto> GetActivitiesAsync(GetActivitiesRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivitiesAsync(requestDto);
    }

    [HttpPost("activity")]
    public async Task<GetActivityDto> GetActivityAsync(GetActivityRequestDto requestDto)
    {
        return await _userActivityAppService.GetActivityAsync(requestDto);
    }

    [AllowAnonymous]
    [HttpGet("getCaHolderCreateTime")]
    public async Task<string> GetCaHolderCreateTimeAsync(GetUserCreateTimeRequestDto requestDto)
    {
        return await _userActivityAppService.GetCaHolderCreateTimeAsync(requestDto);
    }

    [AllowAnonymous]
    [HttpGet("transactions")]
    public async Task<IndexerTransactions> GetTransactionByTransactionType(string transactionType)
    {
        return await _userActivityAppService.GetTransactionByTransactionType(transactionType);
    }

    [AllowAnonymous]
    [HttpPost("transactions/v2")]
    public async Task<IndexerTransactions> GetActivitiesWithBlockHeightAsync([FromBody]TransactionTypeDto request)
    {
        return await _userActivityAppService.GetActivitiesWithBlockHeightAsync(request.Types, request.StartHeight, request.EndHeight);
    }
    
    [AllowAnonymous]
    [HttpPost("transactions/v3")]
    public async Task<IndexerTransactions> GetActivitiesV3Async([FromBody]TransactionTypeDto request)
    {
        return await _userActivityAppService.GetActivitiesV3(request.CaAddressInfos);
    }
}
using System.Threading.Tasks;
using CAServer.EnumType;
using CAServer.Growth;
using CAServer.Growth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Growth")]
[Route("api/app/growth")]
public class GrowthController : CAServerController
{
    private readonly IGrowthAppService _growthAppService;
    private readonly IGrowthStatisticAppService _statisticAppService;

    public GrowthController(IGrowthAppService growthAppService, IGrowthStatisticAppService statisticAppService)
    {
        _growthAppService = growthAppService;
        _statisticAppService = statisticAppService;
    }

    [HttpGet("redDot"), Authorize]
    public async Task<GrowthRedDotDto> GetRedDotAsync()
    {
        return await _growthAppService.GetRedDotAsync();
    }

    [HttpPost("redDot"), Authorize]
    public async Task SetRedDotAsync()
    {
        await _growthAppService.SetRedDotAsync();
    }

    [HttpGet("shortLink"), Authorize]
    public async Task<ShortLinkDto> GetShortLinkAsync(string projectCode)
    {
        return await _growthAppService.GetShortLinkAsync(projectCode);
    }

    [HttpGet("referralInfo"), Authorize(Roles = "admin")]
    public async Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input)
    {
        return await _statisticAppService.GetReferralInfoAsync(input);
    }

    [HttpGet("referralRecordList"),Authorize]
    public async Task<ReferralRecordResponseDto> GetReferralRecordList(ReferralRecordRequestDto input)
    {
        return await _statisticAppService.GetReferralRecordList(input);
    }
    
    [HttpGet("referralRecordRank")]
    public async Task<ReferralRecordsRankResponseDto> GetReferralRecordRankAsync(ReferralRecordRankRequestDto input)
    {
        return await _statisticAppService.GetReferralRecordRankAsync(input);
    }
    
    [HttpGet("activityDetails")]
    public async Task<ActivityDetailsResponseDto> GetActivityDetails(ActivityEnums activityEnums)
    {
        return await _growthAppService.GetActivityDetailsAsync(activityEnums);
    }
    
    [HttpGet("rewardProgress")]
    public async Task<RewardProgressResponseDto> GetRewardProgress(ActivityEnums activityEnums)
    {
        return await _statisticAppService.GetRewardProgressAsync(activityEnums);
    }

    [HttpGet("be-invited-configs")]
    public async Task<BeInvitedConfigResponseDto> GetBeInvitedConfig()
    {
        return await _statisticAppService.GetBeInvitedConfigAsync();
    }
    
    [HttpGet("get-activity-baseInfos")]
    public async Task<ActivityBaseInfoDto> GetActivityBaseInfos()
    {
        return await _statisticAppService.ActivityBaseInfoAsync();
    }
    
    [HttpGet("collect-hamster-userid")]
    public async Task CollectHamsterUserIds(string userId)
    { 
        await _statisticAppService.CollectHamsterUserIdsAsync(userId);
    }
    
    [HttpGet("validate-hamster-score")]
    public async Task<ValidateHamsterScoreResponseDto> ValidateHamsterScore(string userId)
    {
        return await _statisticAppService.ValidateHamsterScoreAsync(userId);
    }
    
    [HttpGet("growthInfos")]
    public async Task<GetGrowthInfosDto> GetGrowthInfosAsync(GetGrowthInfosRequestDto input)
    {
        return await _statisticAppService.GetGrowthInfosAsync(input);
    }

}
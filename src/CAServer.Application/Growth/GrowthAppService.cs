using System;
using System.Threading.Tasks;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.Growth;
using CAServer.Growth.Dtos;
using CAServer.Growth.Etos;
using CAServer.Growth.Provider;
using CAServer.Options;
using CAServer.RedDot;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Growth;

[RemoteService(false), DisableAuditing]
public class GrowthAppService : CAServerAppService, IGrowthAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IRedDotAppService _redDotAppService;
    private readonly INickNameAppService _nickNameAppService;
    private readonly IGrowthProvider _growthProvider;
    private readonly GrowthOptions _growthOptions;

    public GrowthAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IRedDotAppService redDotAppService, INickNameAppService nickNameAppService,
        IOptionsSnapshot<GrowthOptions> growthOptions, IGrowthProvider growthProvider)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _redDotAppService = redDotAppService;
        _nickNameAppService = nickNameAppService;
        _growthProvider = growthProvider;
        _growthOptions = growthOptions.Value;
    }

    public async Task<GrowthRedDotDto> GetRedDotAsync()
    {
        var redDotInfo = await _redDotAppService.GetRedDotInfoAsync(RedDotType.Referral);
        var status = redDotInfo?.Status == RedDotStatus.Read ? RedDotStatus.Read : RedDotStatus.Unread;
        return new GrowthRedDotDto()
        {
            Status = status
        };
    }

    public async Task SetRedDotAsync()
    {
        await _redDotAppService.SetRedDotAsync(RedDotType.Referral);
    }

    public async Task<ShortLinkDto> GetShortLinkAsync(string projectCode)
    {
        var caHash = await GetCaHashAsync();
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UserGrowthPrefix, caHash);
        var growthGrain = _clusterClient.GetGrain<IGrowthGrain>(grainId);

        GrowthGrainDto grainDto;
        var exist = await growthGrain.Exist();
        if (!exist)
        {
            grainDto = await CreateGrowthInfoAsync(growthGrain, CurrentUser.GetId(), projectCode);
        }
        else
        {
            grainDto = await GetGrowthInfoAsync(growthGrain);
        }

        var url = $"{_growthOptions.BaseUrl}/api/app/account/{grainDto.ShortLinkCode}";
        return new ShortLinkDto()
        {
            ShortLink = url
        };
    }

    private async Task<GrowthGrainDto> GetGrowthInfoAsync(IGrowthGrain growthGrain)
    {
        var result = await growthGrain.GetGrowthInfo();
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        return result.Data;
    }

    private async Task<GrowthGrainDto> CreateGrowthInfoAsync(IGrowthGrain growthGrain, Guid userId, string projectCode)
    {
        var caHash = await GetCaHashAsync();
        var shortLinkCode = await GenerateShortLinkCodeAsync(caHash);
        var result = await growthGrain.CreateGrowthInfo(new GrowthGrainDto()
        {
            UserId = userId,
            CaHash = caHash,
            ShortLinkCode = shortLinkCode,
            ProjectCode = projectCode
        });

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<GrowthGrainDto, CreateGrowthEto>(result.Data), false,
            false);
        return result.Data;
    }

    public async Task CreateGrowthInfoAsync(string caHash, ReferralInfo referralInfo)
    {
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UserGrowthPrefix, caHash);
        var growthGrain = _clusterClient.GetGrain<IGrowthGrain>(grainId);
        var shortLinkCode = await GenerateShortLinkCodeAsync(caHash);
        var result = await growthGrain.CreateGrowthInfo(new GrowthGrainDto()
        {
            CaHash = caHash,
            ShortLinkCode = shortLinkCode,
            ReferralCode = referralInfo.ReferralCode,
            ProjectCode = referralInfo.ProjectCode
        });

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<GrowthGrainDto, CreateGrowthEto>(result.Data), false,
            false);
    }

    public async Task<string> GetRedirectUrlAsync(string shortLinkCode)
    {
        var growthInfo = await _growthProvider.GetGrowthInfoByLinkCodeAsync(shortLinkCode);
        if (growthInfo == null)
        {
            throw new UserFriendlyException("user growth info not exist.");
        }

        return
            $"{_growthOptions.RedirectUrl}?referral_code={growthInfo.InviteCode}&project_code={growthInfo.ProjectCode ?? string.Empty}";
    }

    private async Task<string> GetCaHashAsync()
    {
        var caHolder = await _nickNameAppService.GetCaHolderAsync();
        if (caHolder == null || CollectionUtilities.IsNullOrEmpty(caHolder.CaHash))
        {
            throw new UserFriendlyException("user not exist.");
        }

        return caHolder.CaHash;
    }

    private async Task<string> GenerateShortLinkCodeAsync(string plainText)
    {
        var shortLinkCode = MurmurHashHelper.GenerateHash(plainText);
        shortLinkCode = shortLinkCode.Replace("/", "");
        var growthInfo = await _growthProvider.GetGrowthInfoByLinkCodeAsync(shortLinkCode);
        if (growthInfo == null)
        {
            return shortLinkCode;
        }

        plainText += shortLinkCode;
        return await GenerateShortLinkCodeAsync(plainText);
    }
}
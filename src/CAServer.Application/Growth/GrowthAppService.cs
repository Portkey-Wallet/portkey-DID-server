using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.CAAccount;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.Growth;
using CAServer.Growth.Dtos;
using CAServer.Growth.Etos;
using CAServer.RedDot;
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
    private readonly INESTRepository<GrowthIndex, string> _growthRepository;
    private readonly IRedDotAppService _redDotAppService;
    private readonly INickNameAppService _nickNameAppService;

    public GrowthAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        INESTRepository<GrowthIndex, string> growthRepository, IRedDotAppService redDotAppService,
        INickNameAppService nickNameAppService)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _growthRepository = growthRepository;
        _redDotAppService = redDotAppService;
        _nickNameAppService = nickNameAppService;
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

        var url = $"api/app/account/{grainDto.ShortLinkCode}";
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
        var result = await growthGrain.CreateGrowthInfo(new GrowthGrainDto()
        {
            UserId = userId,
            CaHash = caHash,
            ShortLinkCode = MurmurHashHelper.GenerateHash(caHash),
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

    // may be not authrize.
    public async Task CreateGrowthInfoAsync()
    {
        var caHash = await GetCaHashAsync();
        var grainId = GrainIdHelper.GenerateGrainId(CommonConstant.UserGrowthPrefix, caHash);
        var growthGrain = _clusterClient.GetGrain<IGrowthGrain>(grainId);
        var result = await growthGrain.CreateGrowthInfo(new GrowthGrainDto()
        {
            UserId = CurrentUser.GetId(),
            CaHash = caHash,
            ShortLinkCode = MurmurHashHelper.GenerateHash(caHash)
        });

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<GrowthGrainDto, CreateGrowthEto>(result.Data), false,
            false);
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
}
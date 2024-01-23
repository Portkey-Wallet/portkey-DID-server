using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.EnumType;
using CAServer.Grains;
using CAServer.Grains.Grain.Growth;
using CAServer.Growth.Dtos;
using CAServer.Growth.Etos;
using CAServer.Growth.Provider;
using CAServer.Options;
using CAServer.RedDot;
using GraphQL;
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
    private readonly IGraphQLHelper _graphQlHelper;

    public GrowthAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IRedDotAppService redDotAppService, INickNameAppService nickNameAppService,
        IOptionsSnapshot<GrowthOptions> growthOptions, IGrowthProvider growthProvider, IGraphQLHelper graphQlHelper)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _redDotAppService = redDotAppService;
        _nickNameAppService = nickNameAppService;
        _growthProvider = growthProvider;
        _graphQlHelper = graphQlHelper;
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

    // who i invited
    public async Task<ReferralResponseDto> GetReferralInfoAsync(ReferralRequestDto input)
    {
        var result = new ReferralResponseDto();

        foreach (var caHash in input.CaHashes)
        {
            result.ReferralInfos.Add(new Referral()
            {
                CaHash = caHash
            });
        }

        var growthInfos = await _growthProvider.GetGrowthInfosAsync(input.CaHashes, null);
        if (growthInfos.IsNullOrEmpty())
        {
            return result;
        }

        foreach (var growthInfo in growthInfos)
        {
            var referralInfo = result.ReferralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referralInfo.ReferralCode = growthInfo.ReferralCode;
            referralInfo.ProjectCode = growthInfo.ProjectCode;
            referralInfo.InviteCode = growthInfo.InviteCode;
        }

        await GetReferralInfoListAsync(result.ReferralInfos);
        return result;
    }

    private async Task GetReferralInfoListAsync(List<Referral> referralInfos)
    {
        if (referralInfos.IsNullOrEmpty()) return;

        var caHashes = referralInfos.Select(t => t.CaHash).ToList();
        var growthInfos = await _growthProvider.GetGrowthInfosAsync(caHashes, null);
        if (growthInfos.IsNullOrEmpty()) return;

        foreach (var growthInfo in growthInfos)
        {
            var referral = referralInfos.First(t => t.CaHash == growthInfo.CaHash);
            referral.InviteCode = growthInfo.InviteCode;
        }
        
        var inviteCodes = growthInfos.Select(t => t.InviteCode).ToList();
        var indexerReferralInfos = await GetReferralAsync(string.Empty, inviteCodes);

        if (indexerReferralInfos.ReferralInfo.IsNullOrEmpty())
        {
            return;
        }

        foreach (var referralInfo in indexerReferralInfos.ReferralInfo)
        {
            var referral = referralInfos.First(t => t.InviteCode == referralInfo.ReferralCode);
            referral.Children.Add(new Referral()
            {
                CaHash = referralInfo.CaHash,
                ProjectCode = referralInfo.ProjectCode,
                ReferralCode = referralInfo.ReferralCode
            });
        }

        var children = referralInfos.SelectMany(t => t.Children).ToList();
        await GetReferralInfoListAsync(children);
    }

    public async Task<ReferralInfoDto> GetReferralAsync(List<string> caHashes)
    {
        return await _graphQlHelper.QueryAsync<ReferralInfoDto>(new GraphQLRequest
        {
            Query = @"
			      query($caHashes:[String],$referralCodes:[String]) {
              referralInfo(dto: {caHashes:$caHashes,referralCodes:$referralCodes}){
                     caHash,referralCode,projectCode,methodName}
                }",
            Variables = new
            {
                caHashes
            }
        });
    }

    public async Task<ReferralInfoDto> GetReferralAsync(string caHash, List<string> referralCodes)
    {
        return await _graphQlHelper.QueryAsync<ReferralInfoDto>(new GraphQLRequest
        {
            Query = @"
			      query($caHashes:[String],$referralCodes:[String]) {
              referralInfo(dto: {caHashes:$caHashes,referralCodes:$referralCodes}){
                     caHash,referralCode,projectCode,methodName}
                }",
            Variables = new
            {
                caHashes = new List<string>() { caHash }, referralCodes
            }
        });
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
        var growthInfo = await _growthProvider.GetGrowthInfoByLinkCodeAsync(shortLinkCode);
        if (growthInfo == null)
        {
            return shortLinkCode;
        }

        plainText += shortLinkCode;
        return await GenerateShortLinkCodeAsync(plainText);
    }
}
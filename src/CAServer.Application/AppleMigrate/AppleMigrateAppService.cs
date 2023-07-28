using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleAuth.Provider;
using CAServer.AppleMigrate.Dtos;
using CAServer.Commons;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.AppleMigrate;

[RemoteService(false)]
[DisableAuditing]
public class AppleMigrateAppService : CAServerAppService, IAppleMigrateAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IAppleUserProvider _appleUserProvider;
    private readonly IDistributedCache<AppleUserTransfer> _distributedCache;
    private readonly IDistributedCache<AppleMigrateResponseDto> _migrateUserInfo;
    private readonly IDistributedCache<MigrateRecord> _migrateRecord;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;


    public AppleMigrateAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IAppleUserProvider appleUserProvider, IDistributedCache<AppleUserTransfer> distributedCache,
        IDistributedCache<AppleMigrateResponseDto> migrateUserInfo,
        INESTRepository<GuardianIndex, string> guardianRepository,
        IDistributedCache<MigrateRecord> migrateRecord)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _appleUserProvider = appleUserProvider;
        _distributedCache = distributedCache;
        _migrateUserInfo = migrateUserInfo;
        _guardianRepository = guardianRepository;
        _migrateRecord = migrateRecord;
    }

    public async Task<int> MigrateAllAsync(bool retry)
    {
        var count = 0;
        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("all user info not in cache.");
        }

        foreach (var user in userTransfer.AppleUserTransferInfos)
        {
            try
            {
                var userInfo = await _migrateUserInfo.GetAsync(CommonConstant.AppleMigrateUserKey + user.UserId);
                if (userInfo != null && !retry)
                {
                    Logger.LogInformation("user already transferred, userId: {userId}", userInfo.OriginalIdentifier);
                    continue;
                }

                var responseDto = await MigrateAsync(new AppleMigrateRequestDto
                {
                    GuardianIdentifier = user.UserId,
                    MigratedUserId = user.Sub
                });

                if (responseDto == null) continue;

                count++;
            }
            catch (Exception ex)
            {
                Logger.LogError("userId transfer fail, userId: {userId}", user.UserId);
            }
        }

        Logger.LogInformation("#### MigrateAllAsync execute end, migrate count:{count}", count);
        return count;
    }

    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        var guardian = GetGuardian(input.GuardianIdentifier);
        var migratedUserId = input.MigratedUserId;

        var guardianGrainDto = await AddGuardianAsync(guardian, migratedUserId);

        var userInfoDto = await GetUserInfoAsync(input.GuardianIdentifier);
        userInfoDto.Id = input.MigratedUserId;
        if (!input.Email.IsNullOrWhiteSpace())
        {
            userInfoDto.Email = input.Email;
        }

        if (input.IsPrivateEmail)
        {
            userInfoDto.IsPrivateEmail = input.IsPrivateEmail;
        }

        Logger.LogInformation("user extra info : {info}", JsonConvert.SerializeObject(userInfoDto));
        await AddUserInfoAsync(ObjectMapper.Map<UserExtraInfoGrainDto, Verifier.Dtos.UserExtraInfo>(userInfoDto));

        Logger.LogInformation("AddUserInfoAsync success, add userInfo into shared redis.");
        await _appleUserProvider.SetUserExtraInfoAsync(new AppleUserExtraInfo
        {
            UserId = migratedUserId,
            FirstName = userInfoDto.FirstName,
            LastName = userInfoDto.LastName,
        });

        await DeleteGuardian(guardian);

        var responseDto = ObjectMapper.Map<GuardianGrainDto, AppleMigrateResponseDto>(guardianGrainDto);
        if (responseDto == null || responseDto.OriginalIdentifier.IsNullOrWhiteSpace())
        {
            Logger.LogError("something error in guardian grain when migrate user. userId:{userId}",
                input.GuardianIdentifier);

            return null;
        }

        await SetMigrateInfoToCache(responseDto);

        return responseDto;
    }

    private async Task SetMigrateInfoToCache(AppleMigrateResponseDto responseDto)
    {
        await _migrateUserInfo.SetAsync(CommonConstant.AppleMigrateUserKey + responseDto.Identifier,
            responseDto,
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.UtcNow.AddDays(100)
            });

        var record = await _migrateRecord.GetAsync(CommonConstant.AppleMigrateRecordKey) ?? new MigrateRecord();

        var exists = record?.AppleMigrateRecords?.Exists(t => t.Identifier == responseDto.Identifier);

        if (exists == true)
        {
            return;
        }

        record.AppleMigrateRecords.Add(new AppleMigrateRecord()
        {
            Id = responseDto.Id,
            Identifier = responseDto.Identifier,
            OriginalIdentifier = responseDto.OriginalIdentifier
        });

        await _migrateRecord.SetAsync(CommonConstant.AppleMigrateRecordKey,
            record,
            new DistributedCacheEntryOptions()
            {
                AbsoluteExpiration = DateTime.UtcNow.AddDays(100)
            });
    }

    private GuardianGrainDto GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
        if (!guardianGrainDto.Success)
        {
            Logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            throw new UserFriendlyException(guardianGrainDto.Message, GuardianMessageCode.NotExist);
        }

        return guardianGrainDto.Data;
    }

    private async Task<GuardianGrainDto> AddGuardianAsync(GuardianGrainDto guardianGrainDto,
        string migratedUserId)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", migratedUserId);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var resultDto = await guardianGrain.AddGuardianAsync(migratedUserId, guardianGrainDto.Salt,
            guardianGrainDto.IdentifierHash, guardianGrainDto.Identifier);

        if (!resultDto.Success)
        {
            Logger.LogError($"{resultDto.Message} guardianIdentifier: {migratedUserId}");
            throw new UserFriendlyException(resultDto.Message);
        }

        Logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
            JsonConvert.SerializeObject(resultDto.Data));

        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<GuardianGrainDto, GuardianEto>(resultDto.Data), false, false);

        return resultDto.Data;
    }

    private async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);

        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAppleUserAsync(
            ObjectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;
        await _distributedEventBus.PublishAsync(
            ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto), false, false);
    }


    private async Task<UserExtraInfoGrainDto> GetUserInfoAsync(string id)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", id);

        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);
        var resultDto = await userExtraInfoGrain.GetAsync();
        if (!resultDto.Success)
        {
            throw new UserFriendlyException(resultDto.Message);
        }

        return resultDto.Data;
    }

    private async Task DeleteGuardian(GuardianGrainDto grainDto)
    {
        try
        {
            var guardian = ObjectMapper.Map<GuardianGrainDto, GuardianIndex>(grainDto);
            await _guardianRepository.DeleteAsync(guardian);
            Logger.LogDebug($"Guardian delete success: {JsonConvert.SerializeObject(guardian)}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Message}: {Data}", "Guardian add fail",
                JsonConvert.SerializeObject(grainDto));
        }
    }

    public async Task<object> GetFailMigrateUser()
    {
        var userTransfer = await _distributedCache.GetAsync(CommonConstant.AppleUserTransferKey);
        if (userTransfer?.AppleUserTransferInfos == null || userTransfer?.AppleUserTransferInfos.Count <= 0)
        {
            throw new UserFriendlyException("all user info not in cache.");
        }

        var record = await _migrateRecord.GetAsync(CommonConstant.AppleMigrateRecordKey) ?? new MigrateRecord();
        if (record.AppleMigrateRecords.Count <= 0)
        {
            throw new UserFriendlyException("all user migrate info not in cache.");
        }

        var originalIdentifiers = record.AppleMigrateRecords.Select(t => t.OriginalIdentifier).ToList();
        var failUsers = userTransfer.AppleUserTransferInfos.Where(t => !originalIdentifiers.Contains(t.UserId))
            .ToList();

        return failUsers;
    }
}
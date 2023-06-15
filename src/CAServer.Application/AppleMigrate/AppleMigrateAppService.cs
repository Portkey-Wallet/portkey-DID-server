using System.Threading.Tasks;
using CAServer.AppleAuth.Provider;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.AppleMigrate;

[RemoteService(false)]
[DisableAuditing]
public class AppleMigrateAppService : CAServerAppService, IAppleMigrateAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IAppleUserProvider _appleUserProvider;

    public AppleMigrateAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IAppleUserProvider appleUserProvider)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _appleUserProvider = appleUserProvider;
    }

    public async Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input)
    {
        var guardian = GetGuardian(input.GuardianIdentifier);
        var migratedUserId = input.MigratedUserId;

        var guardianGrainDto = await AddGuardianAsync(guardian, migratedUserId);
        if (guardianGrainDto.Success)
        {
            Logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
                JsonConvert.SerializeObject(guardianGrainDto.Data));

            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data));
        }

        var userInfoDto = await GetUserInfoAsync(input.GuardianIdentifier);
        userInfoDto.Id = input.GuardianIdentifier;

        Logger.LogInformation("user extra info : {info}", JsonConvert.SerializeObject(userInfoDto));
        await AddUserInfoAsync(ObjectMapper.Map<UserExtraInfoGrainDto, Verifier.Dtos.UserExtraInfo>(userInfoDto));

        Logger.LogInformation("AddUserInfoAsync success, add userInfo into shared redis.");
        await _appleUserProvider.SetUserExtraInfoAsync(new AppleUserExtraInfo
        {
            UserId = migratedUserId,
            FirstName = userInfoDto.FirstName,
            LastName = userInfoDto.LastName,
        });

        return ObjectMapper.Map<GuardianGrainDto, AppleMigrateResponseDto>(guardianGrainDto.Data);
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

    private async Task<GrainResultDto<GuardianGrainDto>> AddGuardianAsync(GuardianGrainDto guardianGrainDto,
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

        return resultDto;
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
            ObjectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto));
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
}
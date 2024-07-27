using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
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
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GuardianUserProvider(
    ILogger<GuardianUserProvider> logger,
    IClusterClient clusterClient,
    IObjectMapper objectMapper,
    IDistributedEventBus distributedEventBus)
    : CAServerAppService, IGuardianUserProvider
{
    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier)
    {
        var guardianGrainResult = GetGuardian(guardianIdentifier);

        logger.LogInformation("GetGuardian info, guardianIdentifier: {result}",
            JsonConvert.SerializeObject(guardianGrainResult));

        if (guardianGrainResult.Success)
        {
            return Task.FromResult(Tuple.Create(guardianGrainResult.Data.IdentifierHash, guardianGrainResult.Data.Salt, true));
        }

        var salt = GetSalt();
        var identifierHash = GetHash(Encoding.UTF8.GetBytes(guardianIdentifier), salt);

        return Task.FromResult(Tuple.Create(identifierHash.ToHex(), salt.ToHex(), false));
    }
    
    private GrainResultDto<GuardianGrainDto> GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        return guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
    }
    
    private byte[] GetSalt() => Guid.NewGuid().ToByteArray();

    private Hash GetHash(byte[] identifier, byte[] salt)
    {
        const int maxIdentifierLength = 256;
        const int maxSaltLength = 16;

        if (identifier.Length > maxIdentifierLength)
        {
            throw new Exception("Identifier is too long");
        }

        if (salt.Length != maxSaltLength)
        {
            throw new Exception($"Salt has to be {maxSaltLength} bytes.");
        }

        var hash = HashHelper.ComputeFrom(identifier);
        return HashHelper.ComputeFrom(hash.Concat(salt).ToArray());
    }

    public async Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.AddGuardianAsync(guardianIdentifier, salt, identifierHash);

        logger.LogInformation("AddGuardianAsync result: {result}", JsonConvert.SerializeObject(guardianGrainDto));
        if (guardianGrainDto.Success)
        {
            logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
                JsonConvert.SerializeObject(guardianGrainDto.Data));
            
            await distributedEventBus.PublishAsync(
                ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data));
        }
    }

    public async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);
        var userExtraInfoGrain = clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);

        var grainDto = await userExtraInfoGrain.AddOrUpdateAsync(
            objectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;

        Logger.LogInformation("Add or update user extra info success, Publish to MQ: {data}",
            JsonConvert.SerializeObject(userExtraInfo));

        var userExtraInfoEto = objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto);
        logger.LogDebug("Publish user extra info to mq: {data}", JsonConvert.SerializeObject(userExtraInfoEto));
        await distributedEventBus.PublishAsync(userExtraInfoEto);
    }
}
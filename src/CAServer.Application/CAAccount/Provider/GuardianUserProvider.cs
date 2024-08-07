using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class GuardianUserProvider
    : CAServerAppService, IGuardianUserProvider
{
    private readonly ILogger<GuardianUserProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;

    public GuardianUserProvider(
        ILogger<GuardianUserProvider> logger,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus,
        INESTRepository<GuardianIndex, string> guardianRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _guardianRepository = guardianRepository;
    }

    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier,
        string guardianIdentifierHash, string guardianSalt)
    {
        var guardianGrainResult = GetGuardian(guardianIdentifier);

        _logger.LogInformation("GetGuardian info, guardianIdentifier: {result}",
            JsonConvert.SerializeObject(guardianGrainResult));

        if (guardianGrainResult.Success)
        {
            return Task.FromResult(Tuple.Create(guardianGrainResult.Data.IdentifierHash, guardianGrainResult.Data.Salt,
                true));
        }

        var salt = guardianSalt.IsNullOrEmpty()
            ? GetSalt()
            : ByteArrayHelper.HexStringToByteArray(guardianSalt);
        var identifierHash = guardianIdentifierHash.IsNullOrEmpty()
            ? GetHash(Encoding.UTF8.GetBytes(guardianIdentifier), salt)
            : Hash.LoadFromHex(guardianIdentifierHash);

        return Task.FromResult(Tuple.Create(identifierHash.ToHex(), salt.ToHex(), false));
    }

    private GrainResultDto<GuardianGrainDto> GetGuardian(string guardianIdentifier)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
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
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = await guardianGrain.AddGuardianAsync(guardianIdentifier, salt, identifierHash);

        _logger.LogInformation("AddGuardianAsync result: {result}", JsonConvert.SerializeObject(guardianGrainDto));
        if (guardianGrainDto.Success)
        {
            _logger.LogInformation("Add guardian success, prepare to publish to mq: {data}",
                JsonConvert.SerializeObject(guardianGrainDto.Data));

            await _distributedEventBus.PublishAsync(
                ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data));
        }
    }

    public async Task AddUserInfoAsync(Verifier.Dtos.UserExtraInfo userExtraInfo)
    {
        var userExtraInfoGrainId =
            GrainIdHelper.GenerateGrainId("UserExtraInfo", userExtraInfo.Id);
        var userExtraInfoGrain = _clusterClient.GetGrain<IUserExtraInfoGrain>(userExtraInfoGrainId);
        var userInfoResultDto = await userExtraInfoGrain.GetAsync();
        if (userInfoResultDto != null && userInfoResultDto.Success)
        {
            return;
        }

        var grainDto = await userExtraInfoGrain.AddOrUpdateAsync(
            _objectMapper.Map<Verifier.Dtos.UserExtraInfo, UserExtraInfoGrainDto>(userExtraInfo));

        grainDto.Id = userExtraInfo.Id;

        Logger.LogInformation("Add or update user extra info success, Publish to MQ: {data}",
            JsonConvert.SerializeObject(userExtraInfo));

        var userExtraInfoEto = _objectMapper.Map<UserExtraInfoGrainDto, UserExtraInfoEto>(grainDto);
        _logger.LogDebug("Publish user extra info to mq: {data}", JsonConvert.SerializeObject(userExtraInfoEto));
        await _distributedEventBus.PublishAsync(userExtraInfoEto);
    }

    public async Task<bool> AppendGuardianPoseidonHashAsync(string guardianIdentifier, string identifierPoseidonHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var existedGuardian = await guardianGrain.GetGuardianAsync(guardianIdentifier);
        if (!existedGuardian.Success || existedGuardian.Data == null)
        {
            _logger.LogError("get guardian from mongodb error, guardianIdentifier:{0},identifierPoseidonHash:{1}", guardianIdentifier, identifierPoseidonHash);
            return false;
        }
        if (!existedGuardian.Data.IdentifierPoseidonHash.IsNullOrEmpty() && identifierPoseidonHash.Equals(existedGuardian.Data.IdentifierPoseidonHash))
        {
            return true;
        }
        var guardianGrainDto =
            await guardianGrain.AppendGuardianPoseidonHashAsync(guardianIdentifier, identifierPoseidonHash);
        if (guardianGrainDto.Success)
        {
            _logger.LogInformation("Append guardian poseidon hash success: {data}", JsonConvert.SerializeObject(guardianGrainDto.Data));
            
            var eventData= ObjectMapper.Map<GuardianGrainDto, GuardianEto>(guardianGrainDto.Data);
            try
            {
                var guardian = _objectMapper.Map<GuardianEto, GuardianIndex>(eventData);
                await _guardianRepository.AddOrUpdateAsync(guardian);
            
                _logger.LogDebug("Guardian add or update success, id: {id}", eventData.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Message}: {Data}", "Guardian add fail",
                    JsonConvert.SerializeObject(eventData));
                throw new UserFriendlyException("save guardian in es error");
            }
        }
        else
        {
            _logger.LogInformation("AppendGuardianPoseidonHashAsync failed result: {result}",
                JsonConvert.SerializeObject(guardianGrainDto));
        }

        return true;
    }
    
    public async Task<List<GuardianIndexDto>> GetGuardianListAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsDeleted).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false).ToList();

        return ObjectMapper.Map<List<GuardianIndex>, List<GuardianIndexDto>>(result);
    }
}
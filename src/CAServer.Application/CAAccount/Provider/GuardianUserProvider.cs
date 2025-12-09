using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.CAAccount.Dtos;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Grains.Grain.UserExtraInfo;
using CAServer.Guardian;
using CAServer.Verifier.Dtos;
using CAServer.Verifier.Etos;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Identity;
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
    private readonly IPoseidonIdentifierHashProvider _poseidonProvider;
    private readonly ICAAccountProvider _accountProvider;
    private readonly IdentityUserManager _userManager;

    public GuardianUserProvider(
        ILogger<GuardianUserProvider> logger,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus,
        INESTRepository<GuardianIndex, string> guardianRepository,
        IPoseidonIdentifierHashProvider poseidonProvider,
        ICAAccountProvider accountProvider,
        IdentityUserManager userManager)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _guardianRepository = guardianRepository;
        _poseidonProvider = poseidonProvider;
        _accountProvider = accountProvider;
        _userManager = userManager;
    }

    public Task<Tuple<string, string, bool>> GetSaltAndHashAsync(string guardianIdentifier, string guardianSalt, string poseidonHash)
    {
        var guardianGrainResult = GetGuardian(guardianIdentifier);

        _logger.LogInformation("GetGuardian info guardianIdentifier:{0}, salt:{1}, poseidon:{2}, guardianIdentifier: {3}",
            guardianIdentifier, guardianSalt, poseidonHash, JsonConvert.SerializeObject(guardianGrainResult));
        if (guardianGrainResult.Success && guardianGrainResult.Data != null)
        {
            return Task.FromResult(Tuple.Create(guardianGrainResult.Data.IdentifierHash, guardianGrainResult.Data.Salt, true));
        }

        if (guardianSalt.IsNullOrEmpty())
        {
            throw new UserFriendlyException("the guardian's salt is invalid");
        }
        var salt = ByteArrayHelper.HexStringToByteArray(guardianSalt);
        var identifierHash = GetHash(Encoding.UTF8.GetBytes(guardianIdentifier), salt);
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

    public async Task AddGuardianAsync(string guardianIdentifier, string salt, string identifierHash, string poseidonHash)
    {
        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);
        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        if (poseidonHash.IsNullOrEmpty())
        {
            poseidonHash = _poseidonProvider.GenerateIdentifierHash(guardianIdentifier, ByteArrayHelper.HexStringToByteArray(salt));
        }
        var guardianGrainDto = await guardianGrain.AddGuardianWithPoseidonHashAsync(guardianIdentifier, salt, identifierHash, poseidonHash);

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
        if (userInfoResultDto is { Success: true })
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
    
    public async Task AppendSecondaryEmailInfo(VerifyTokenRequestDto requestDto, string guardianIdentifierHash,
        string guardianIdentifier, GuardianIdentifierType type)
    {
        if (requestDto.CaHash.IsNullOrEmpty())
        {
            //existed guardian, get secondary email by guardian's identifierHash
            requestDto.SecondaryEmail = await GetSecondaryEmailAsync(guardianIdentifierHash);
        }
        else
        {
            //add guardian operation, get secondary email by caHash
            requestDto.SecondaryEmail = await GetSecondaryEmailByCaHash(requestDto.CaHash);
        }
        requestDto.GuardianIdentifier = guardianIdentifier;
        requestDto.Type = type;
    }

    private async Task<string> GetSecondaryEmailByCaHash(string caHash)
    {
        var userId = await GetUserId(caHash);
        var caHolderGrain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolder = await caHolderGrain.GetCaHolder();
        if (!caHolder.Success || caHolder.Data == null)
        {
            throw new UserFriendlyException(caHolder.Message);
        }
        return caHolder.Data.SecondaryEmail;
    }
    
    private async Task<Guid> GetUserId(string caHash)
    {
        var user = await _userManager.FindByNameAsync(caHash);
        if (user != null)
        {
            return user.Id;
        }
        throw new UserFriendlyException("the user doesn't exist, caHash:" + caHash);
    }

    private async Task<string> GetSecondaryEmailAsync(string guardianIdentifierHash)
    {
        if (guardianIdentifierHash.IsNullOrEmpty())
        {
            return string.Empty;
        }
        var guardianIndex = await _accountProvider.GetIdentifiersAsync(guardianIdentifierHash);
        return guardianIndex != null ? guardianIndex.SecondaryEmail : string.Empty;
    }
}
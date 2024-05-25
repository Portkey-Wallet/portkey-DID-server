using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Grains.Grain.Guardian;
using CAServer.Guardian.Provider;
using CAServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using Orleans;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

namespace CAServer.Guardian;

[RemoteService(false)]
[DisableAuditing]
public class GuardianAppService : CAServerAppService, IGuardianAppService
{
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    private readonly ILogger<GuardianAppService> _logger;
    private readonly ChainOptions _chainOptions;
    private readonly IGuardianProvider _guardianProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IAppleUserProvider _appleUserProvider;
    private readonly AppleTransferOptions _appleTransferOptions;
    private readonly StopRegisterOptions _stopRegisterOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IImRequestProvider _imRequestProvider;
    private readonly HostInfoOptions _hostInfoOptions;

    public GuardianAppService(
        INESTRepository<GuardianIndex, string> guardianRepository, IAppleUserProvider appleUserProvider,
        INESTRepository<UserExtraInfoIndex, string> userExtraInfoRepository, ILogger<GuardianAppService> logger,
        IOptions<ChainOptions> chainOptions, IGuardianProvider guardianProvider, IClusterClient clusterClient,
        IOptionsSnapshot<AppleTransferOptions> appleTransferOptions,
        IOptionsSnapshot<StopRegisterOptions> stopRegisterOptions,
        IObjectMapper objectMapper, INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IImRequestProvider imRequestProvider, IOptionsSnapshot<HostInfoOptions> hostInfoOptions)
    {
        _guardianRepository = guardianRepository;
        _userExtraInfoRepository = userExtraInfoRepository;
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _guardianProvider = guardianProvider;
        _clusterClient = clusterClient;
        _appleUserProvider = appleUserProvider;
        _appleTransferOptions = appleTransferOptions.Value;
        _stopRegisterOptions = stopRegisterOptions.Value;
        _objectMapper = objectMapper;
        _caHolderRepository = caHolderRepository;
        _imRequestProvider = imRequestProvider;
        _hostInfoOptions = hostInfoOptions.Value;
    }

    public async Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto)
    {
        var hash = "";
        if (!guardianIdentifierDto.GuardianIdentifier.IsNullOrWhiteSpace())
        {
            hash = await GetHashFromIdentifierAsync(guardianIdentifierDto.GuardianIdentifier);

            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new UserFriendlyException($"{guardianIdentifierDto.GuardianIdentifier} not exist.",
                    GuardianMessageCode.NotExist);
            }
        }
        var holderInfo = await GetHolderInfosAsync(hash, guardianIdentifierDto.ChainId, guardianIdentifierDto.CaHash,
            guardianIdentifierDto.GuardianIdentifier);

        var guardianResult =
            ObjectMapper.Map<GetHolderInfoOutput, GuardianResultDto>(holderInfo);

        if (guardianResult.GuardianList?.Guardians?.Count == 0 ||
            (!guardianResult.CreateChainId.IsNullOrWhiteSpace() &&
             guardianResult.CreateChainId != guardianIdentifierDto.ChainId))
        {
            throw new UserFriendlyException("This address is already registered on another chain.", "20004");
        }

        var identifierHashList = holderInfo.GuardianList.Guardians.Select(t => t.IdentifierHash.ToHex()).ToList();

        var hashDic = await GetIdentifiersAsync(identifierHashList);
        var identifiers = hashDic?.Values.ToList();

        var userExtraInfos = await GetUserExtraInfoAsync(identifiers);

        await AddGuardianInfoAsync(guardianResult.GuardianList?.Guardians, hashDic, userExtraInfos);
        return guardianResult;
    }

    public async Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto)
    {
        if (_appleTransferOptions.IsNeedIntercept(requestDto.LoginGuardianIdentifier))
        {
            throw new UserFriendlyException(_appleTransferOptions.ErrorMessage);
        }

        var guardianIdentifierHash = GetHash(requestDto.LoginGuardianIdentifier);
        var guardians = await _guardianProvider.GetGuardiansAsync(guardianIdentifierHash, requestDto.CaHash);
        var guardian = guardians?.CaHolderInfo?.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.OriginChainId));

        var originChainId = guardian == null
            ? await GetOriginChainIdAsync(guardianIdentifierHash, requestDto.CaHash)
            : guardian.OriginChainId;

        return new RegisterInfoResultDto { OriginChainId = originChainId };
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

    private string GetHash(string guardianIdentifier)
    {
        if (string.IsNullOrWhiteSpace(guardianIdentifier)) return string.Empty;

        var guardianGrainId = GrainIdHelper.GenerateGrainId("Guardian", guardianIdentifier);

        var guardianGrain = _clusterClient.GetGrain<IGuardianGrain>(guardianGrainId);
        var guardianGrainDto = guardianGrain.GetGuardianAsync(guardianIdentifier).Result;
        if (!guardianGrainDto.Success)
        {
            _logger.LogError($"{guardianGrainDto.Message} guardianIdentifier: {guardianIdentifier}");
            if (_stopRegisterOptions.Open)
            {
                throw new UserFriendlyException(_stopRegisterOptions.Message, GuardianMessageCode.StopRegister);
            }
            throw new UserFriendlyException(guardianGrainDto.Message, GuardianMessageCode.NotExist);
        }

        return guardianGrainDto.Data.IdentifierHash;
    }

    private async Task<string> GetOriginChainIdAsync(string guardianIdentifierHash, string caHash)
    {
        foreach (var (chainId, chainInfo) in _chainOptions.ChainInfos)
        {
            try
            {
                var holderInfo =
                    await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
                if (holderInfo.CreateChainId > 0)
                {
                    return ChainHelper.ConvertChainIdToBase58(holderInfo.CreateChainId);
                }

                if (holderInfo?.GuardianList?.Guardians?.Count > 0)
                {
                    return chainId;
                }
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("Not found ca_hash"))
                {
                    _logger.LogError(e, "GetRegisterHolderInfoAsync: guardian hash call contract GetHolderInfo fail.");
                    throw new UserFriendlyException(e.Message);
                }
            }
        }
        
        if (_stopRegisterOptions.Open)
        {
            throw new UserFriendlyException(_stopRegisterOptions.Message, GuardianMessageCode.StopRegister);
        }
        throw new UserFriendlyException("This address is not registered.", GuardianMessageCode.NotExist);
    }

    private async Task AddGuardianInfoAsync(List<GuardianDto> guardians, Dictionary<string, string> hashDic,
        List<UserExtraInfoIndex> userExtraInfos)
    {
        if (guardians == null || guardians.Count == 0)
        {
            return;
        }

        foreach (var guardian in guardians)
        {
            guardian.GuardianIdentifier = hashDic.GetValueOrDefault(guardian.IdentifierHash);

            var extraInfo = userExtraInfos?.FirstOrDefault(f => f.Id == guardian.GuardianIdentifier);
            if (extraInfo != null)
            {
                guardian.ThirdPartyEmail = extraInfo.Email;
                var guardianType = Enum.Parse(typeof(GuardianIdentifierType), guardian.Type);
                switch (guardianType)
                {
                    case GuardianIdentifierType.Google:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        break;
                    case GuardianIdentifierType.Telegram:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        guardian.IsPrivate = true;
                        break;
                    case GuardianIdentifierType.Apple:
                        await SetNameAsync(guardian);
                        guardian.IsPrivate = extraInfo.IsPrivateEmail;
                        break;
                    case GuardianIdentifierType.Twitter:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.IsPrivate = true;
                        break;
                    case GuardianIdentifierType.Facebook:
                        guardian.FirstName = extraInfo.FirstName;
                        guardian.LastName = extraInfo.LastName;
                        guardian.IsPrivate = true;
                        guardian.ImageUrl = extraInfo.Picture;
                        break;
                }
            }
        }
    }


    private async Task<GetHolderInfoOutput> GetHolderInfosAsync(string guardianIdentifierHash, string chainId,
        string caHash, string guardianIdentifier)
    {
        try
        {
            return await _guardianProvider.GetHolderInfoFromContractAsync(guardianIdentifierHash, caHash, chainId);
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("Not found ca_hash"))
            {
                _logger.LogError(ex, "{Message}, {Data}", "guardian hash call contract GetHolderInfo fail.",
                    $"guardianIdentifierHash={guardianIdentifierHash ?? ""}, chainId={chainId ?? ""}, caHash={caHash ?? ""}, guardianIdentifier={guardianIdentifier ?? ""}");
                throw new UserFriendlyException(ex.Message);
            }

            if (!string.IsNullOrWhiteSpace(caHash))
            {
                throw new UserFriendlyException($"{caHash} not exist.",
                    GuardianMessageCode.NotExist);
            }

            throw new UserFriendlyException($"{guardianIdentifier ?? "identifier"} not exist.",
                GuardianMessageCode.NotExist);
        }
    }

    private async Task<Dictionary<string, string>> GetIdentifiersAsync(List<string> identifierHashList)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>
        {
            q => q.Terms(i => i.Field(f => f.IdentifierHash).Terms(identifierHashList))
        };

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardians = await _guardianRepository.GetListAsync(Filter);

        var result = guardians.Item2.Where(t => t.IsDeleted == false);

        return result.ToDictionary(t => t.IdentifierHash, t => t.Identifier);
    }


    private async Task<string> GetHashFromIdentifierAsync(string guardianIdentifier)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GuardianIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.Identifier).Value(guardianIdentifier))
        };

        QueryContainer Filter(QueryContainerDescriptor<GuardianIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var guardianGrainDto = await _guardianRepository.GetAsync(Filter);
        return guardianGrainDto == null || guardianGrainDto.IsDeleted ? null : guardianGrainDto.IdentifierHash;
    }


    private async Task<List<UserExtraInfoIndex>> GetUserExtraInfoAsync(List<string> identifiers)
    {
        try
        {
            if (identifiers == null || identifiers.Count == 0)
            {
                return new List<UserExtraInfoIndex>();
            }

            var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraInfoIndex>, QueryContainer>>
            {
                q => q.Terms(i => i.Field(f => f.Id).Terms(identifiers))
            };

            QueryContainer Filter(QueryContainerDescriptor<UserExtraInfoIndex> f) =>
                f.Bool(b => b.Must(mustQuery));

            var userExtraInfos = await _userExtraInfoRepository.GetListAsync(Filter);

            return userExtraInfos.Item2;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "in GetUserExtraInfoAsync");
        }

        return new List<UserExtraInfoIndex>();
    }

    private async Task SetNameAsync(GuardianDto guardian)
    {
        var userInfo = await _appleUserProvider.GetUserExtraInfoAsync(guardian.GuardianIdentifier);
        if (userInfo != null)
        {
            guardian.FirstName = userInfo.FirstName;
            guardian.LastName = userInfo.LastName;
        }
    }

    public async Task<bool> UpdateUnsetGuardianIdentifierAsync(UpdateGuardianIdentifierDto updateGuardianIdentifierDto)
    {
        GuardianResultDto guardianResultDto = await GetGuardianIdentifiersAsync(updateGuardianIdentifierDto);
        if (guardianResultDto == null || guardianResultDto.GuardianList == null || guardianResultDto.GuardianList.Guardians.IsNullOrEmpty())
        {
            return false;
        }
        var result = await ModifyNicknameHandler(guardianResultDto, updateGuardianIdentifierDto.UserId, updateGuardianIdentifierDto.UnsetGuardianIdentifierHash);
        _logger.LogInformation("UpdateUnsetGuardianIdentifierAsync result is={0}, caHash={1}", result, updateGuardianIdentifierDto.CaHash);
        return result;
    }

    private async Task<bool> ModifyNicknameHandler(GuardianResultDto guardianResultDto, Guid userId, string unsetGuardianIdentifierHash)
    {
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
        var caHolderGrainDto = await grain.GetCaHolder();
        if (!caHolderGrainDto.Success)
        {
            return false;
        }
        if (caHolderGrainDto.Data == null)
        {
            return false;
        }

        var caHolder = caHolderGrainDto.Data;
        var modifiedNickname = caHolder.ModifiedNickname;
        var identifierHashFromGrain = caHolder.IdentifierHash;
        if (modifiedNickname && identifierHashFromGrain.IsNullOrEmpty())
        {
            return false;
        }
        if (identifierHashFromGrain.IsNullOrEmpty() || !identifierHashFromGrain.Equals(unsetGuardianIdentifierHash))
        {
            return false;
        }
        var guardians = guardianResultDto.GuardianList.Guardians;
        var guardianDto = guardians.FirstOrDefault(g => g.IsLoginGuardian);
        if (guardianDto == null)
        {
            return false;
        }

        string changedNickname;
        string nickname = userId.ToString("N").Substring(0, 8);
        if ("Telegram".Equals(guardianDto.Type) || "Twitter".Equals(guardianDto.Type) || "Facebook".Equals(guardianDto.Type))
        {
            changedNickname = GetFirstNameFormat(nickname, guardianDto.FirstName, guardianResultDto.CaAddress);
        }
        else if ("Email".Equals(guardianDto.Type) && !guardianDto.GuardianIdentifier.IsNullOrEmpty())
        {
            changedNickname = GetEmailFormat(nickname, guardianDto.GuardianIdentifier, guardianDto.FirstName, guardianResultDto.CaAddress);
        }
        else
        {
            changedNickname = GetEmailFormat(nickname, guardianDto.ThirdPartyEmail, guardianDto.FirstName, guardianResultDto.CaAddress);
        }
        _logger.LogInformation("UpdateUnsetGuardianIdentifierAsync cahash={0} nickname={1}, changedNickname={2}", guardianResultDto.CaAddress, nickname, changedNickname);
        GrainResultDto<CAHolderGrainDto> result = null;
        if (changedNickname.IsNullOrEmpty())
        {
            result = await grain.UpdateNicknameAndMarkBitAsync(nickname, false, string.Empty);
        }
        else
        {
            result = await grain.UpdateNicknameAndMarkBitAsync(changedNickname, true, guardianDto.IdentifierHash);
        }
        _logger.LogInformation("UpdateUnsetGuardianIdentifierAsync update result={0}", JsonConvert.SerializeObject(result.Data));
        if (!result.Success)
        {
            _logger.LogError("update user nick name failed, nickname={0}, changedNickname={1}", nickname, changedNickname);
            return false;
        }
        //update es
        try
        {
            await _caHolderRepository.UpdateAsync(_objectMapper.Map<CAHolderGrainDto, CAHolderIndex>(result.Data));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateUnsetGuardianIdentifierAsync update es caholder failed, userid={1}, nickname={0}", userId, changedNickname);
        }
        //update im user
        try
        {
            await UpdateImUserAsync(userId, changedNickname, caHolder.Avatar);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateUnsetGuardianIdentifierAsync update im user failed, userid={1}, nickname={0}", userId, changedNickname);
        }
        return true;
    }
    
    private async Task UpdateImUserAsync(Guid userId, string nickName, string avatar = "")
    {
        if (_hostInfoOptions.Environment == Options.Environment.Development)
        {
            return;
        }

        var imUserUpdateDto = new ImUserUpdateDto
        {
            Name = nickName,
            Avatar = avatar
        };

        try
        {
            await _imRequestProvider.PostAsync<object>(ImConstant.UpdateImUserUrl, imUserUpdateDto);
            Logger.LogInformation("{userId} update im user : {name}", userId.ToString(), nickName);
        }
        catch (Exception e)
        {
            Logger.LogError(e, ImConstant.ImServerErrorPrefix + " update im user fail : {userId}, {name}",
                userId.ToString(), nickName);
        }
    }
    
    private string GetFirstNameFormat(string nickname, string firstName, string address)
    {
        if (firstName.IsNullOrEmpty() && address.IsNullOrEmpty())
        {
            return nickname;
        }
        if (!firstName.IsNullOrEmpty() && Regex.IsMatch(firstName,"^\\w+$"))
        {
            return firstName + "***";
        }
    
        if (!address.IsNullOrEmpty())
        {
            int length = address.Length;
            return address.Substring(0, 3) + "***" + address.Substring(length - 3);
        }
        return nickname;
    }

    private string GetEmailFormat(string nickname, string guardianIdentifier, string firstName, string address)
    {
        if (guardianIdentifier.IsNullOrEmpty())
        {
            return GetFirstNameFormat(nickname, firstName, address);
        }

        int index = guardianIdentifier.LastIndexOf("@");
        if (index < 0)
        {
            return nickname;
        }

        string frontPart = guardianIdentifier.Substring(0, index);
        string backPart = guardianIdentifier.Substring(index);
        int frontLength = frontPart.Length;
        if (frontLength > 4)
        {
            return frontPart.Substring(0, 4) + "***" + backPart;
        }
        else
        {
            return frontPart + "***" + backPart;
        }
    }
}
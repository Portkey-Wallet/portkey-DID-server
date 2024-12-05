using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Grains.Grain;
using CAServer.Grains.Grain.Contacts;
using CAServer.Guardian;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.CAAccount.Provider;

public interface INicknameProvider
{
    public Task<bool> ModifyNicknameHandler(GuardianResultDto guardianResultDto, Guid userId, CAHolderGrainDto caHolder);
}

public class NicknameProvider : INicknameProvider, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GuardianAppService> _logger;

    public NicknameProvider(IObjectMapper objectMapper, INESTRepository<CAHolderIndex, Guid> caHolderRepository,
        IClusterClient clusterClient, ILogger<GuardianAppService> logger
        )
    {
        _objectMapper = objectMapper;
        _caHolderRepository = caHolderRepository;
        _clusterClient = clusterClient;
        _logger = logger;
    }
    
    public async Task<bool> ModifyNicknameHandler(GuardianResultDto guardianResultDto, Guid userId, CAHolderGrainDto caHolder)
    {
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
        var grain = _clusterClient.GetGrain<ICAHolderGrain>(userId);
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

        return true;
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
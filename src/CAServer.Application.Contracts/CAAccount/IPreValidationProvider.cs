using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Guardian;

namespace CAServer.CAAccount;

public interface IPreValidationProvider
{
    public Task<bool> ValidateSocialRecovery(RequestSource source, string caHash,
        string chainId, string manager, List<GuardianInfo> guardiansApproved, List<ManagerInfoDto> existedManagers);

    public Task SaveManagerInCache(string manager, string caHash, string caAddress, string chainId);
    
    public Task<ManagerCacheDto> GetManagerFromCache(string manager);
}
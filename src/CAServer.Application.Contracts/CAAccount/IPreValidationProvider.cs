using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;

namespace CAServer.CAAccount;

public interface IPreValidationProvider
{
    public Task<bool> ValidateSocialRecovery(RequestSource source, string caHash,
        string chainId, string manager, List<GuardianInfo> guardiansApproved, List<ManagerDto> existedManagers);

    public Task SaveManagerInCache(string manager, string caHash, string caAddress);
    
    public Task<ManagerCacheDto> GetManagerFromCache(string manager);
}
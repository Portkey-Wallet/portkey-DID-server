using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;

namespace CAServer.CAAccount;

public interface ICAAccountAppService
{
    Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input);
    Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input);
    Task<RevokeEntranceResultDto> RevokeEntranceAsync();
    Task<CancelCheckResultDto> CancelCheckAsync(Guid uid);
    Task<CancelResultDto> CancelRequestAsync(CancelRequestDto input);
}
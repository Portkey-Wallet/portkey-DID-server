using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;

namespace CAServer.CAAccount;

public interface ICAAccountAppService
{
    Task<AccountResultDto> RegisterRequestAsync(RegisterRequestDto input);
    Task<AccountResultDto> RecoverRequestAsync(RecoveryRequestDto input);
    Task<CancelCheckResultDto> CancelEntranceAsync();
    Task<CancelCheckResultDto> CancelCheckAsync(CancelCheckDto input);
    Task<CancelResultDto> CancelRequestAsync(CancelRequestDto input);
}
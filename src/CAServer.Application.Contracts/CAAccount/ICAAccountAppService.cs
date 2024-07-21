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
    Task<CancelCheckResultDto> RevokeCheckAsync(Guid uId);
    Task<RevokeResultDto> RevokeAsync(RevokeDto input);
    Task<CheckManagerCountResultDto> CheckManagerCountAsync(string caHash);
    Task<AuthorizeDelegateResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeRequestDto input);
    Task<RevokeResultDto> RevokeAccountAsync(RevokeAccountInput input);
    Task<CancelCheckResultDto> RevokeValidateAsync(Guid userId, string type);

    Task TestCreateHolderInfoAsync(RegisterDto registerDto);

    Task<CAHolderExistsResponseDto> VerifyCaHolderExistByAddressAsync(string address);

}
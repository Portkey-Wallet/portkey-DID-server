using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;

namespace CAServer.Guardian;

public interface IGuardianAppService
{
    Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto);
    Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto);
    TransactionFeeDto GetTransactionFee(string type);
}
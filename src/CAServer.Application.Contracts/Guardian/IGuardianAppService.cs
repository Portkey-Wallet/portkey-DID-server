using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;

namespace CAServer.Guardian;

public interface IGuardianAppService
{
    Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto);
    Task<RegisterInfoResultDto> GetRegisterInfoAsync(RegisterInfoDto requestDto);
    Task<List<GuardianIndexDto>> GetGuardianListAsync(List<string> identifierHashList);
}
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CAServer.Guardian;

public interface IGuardianAppService
{
    Task<GuardianResultDto> GetGuardianIdentifiersAsync(GuardianIdentifierDto guardianIdentifierDto);
}
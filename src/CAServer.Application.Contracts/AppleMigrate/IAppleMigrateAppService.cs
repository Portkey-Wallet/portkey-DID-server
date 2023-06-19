using System.Threading.Tasks;
using CAServer.AppleMigrate.Dtos;

namespace CAServer.AppleMigrate;

public interface IAppleMigrateAppService
{
    Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input);
}
using System.Threading.Tasks;

namespace CAServer.AppleMigrate;

public interface IAppleMigrateAppService
{
    Task<AppleMigrateResponseDto> MigrateAsync(AppleMigrateRequestDto input);
}
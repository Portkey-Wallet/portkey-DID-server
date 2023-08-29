using System.Threading.Tasks;
using CAServer.Security.Dtos;

namespace CAServer.Security;

public interface IUserSecurityAppService
{
    public Task<TransferLimitListResultDto> GetTransferLimitListByCaHashAsync(
        GetTransferLimitListByCaHashAsyncDto input);
}
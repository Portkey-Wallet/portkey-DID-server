using System.Threading.Tasks;
using CAServer.ImTransfer.Dtos;

namespace CAServer.ImTransfer;

public interface IImTransferAppService
{
    Task<ImTransferResponseDto> TransferAsync(ImTransferDto input);
    Task<TransferResultDto> GetTransferResultAsync(string transferId);
}
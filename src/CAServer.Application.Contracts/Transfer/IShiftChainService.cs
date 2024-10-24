using System.Threading.Tasks;
using CAServer.Transfer.Dtos;
using Volo.Abp.Application.Dtos;

namespace CAServer.Transfer;

public interface IShiftChainService
{
    Task Init();
    Task<ResponseWrapDto<ReceiveNetworkDto>> GetReceiveNetworkList(GetReceiveNetworkListRequestDto request);
    Task<ResponseWrapDto<SendNetworkDto>> GetSendNetworkList(GetSendNetworkListRequestDto request);
}
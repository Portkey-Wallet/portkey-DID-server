using System.Threading.Tasks;
using CAServer.AddressBook.Dtos;
using CAServer.Transfer.Dtos;

namespace CAServer.Transfer;

public interface IShiftChainService
{
    Task Init();
    Task<ResponseWrapDto<ReceiveNetworkDto>> GetReceiveNetworkList(GetReceiveNetworkListRequestDto request);
    Task<ResponseWrapDto<SendNetworkDto>> GetSendNetworkList(GetSendNetworkListRequestDto request);
    Task<GetSupportNetworkDto> GetSupportNetworkListAsync();
}
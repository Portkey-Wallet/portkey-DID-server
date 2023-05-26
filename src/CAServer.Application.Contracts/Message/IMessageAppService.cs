using System.Threading.Tasks;
using CAServer.Message.Dtos;
using CAServer.ThirdPart.Dtos;
using AlchemyTargetAddressDto = CAServer.Message.Dtos.AlchemyTargetAddressDto;

namespace CAServer.Message;

public interface IMessageAppService
{
    Task ScanLoginSuccess(ScanLoginDto request);
    Task AlchemyTargetAddress(AlchemyTargetAddressDto request);
}
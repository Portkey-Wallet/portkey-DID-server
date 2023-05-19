using System.Threading.Tasks;
using CAServer.Message.Dtos;

namespace CAServer.Message;

public interface IMessageAppService
{
    Task ScanLoginSuccess(ScanLoginDto request);
    Task AlchemyTargetAddress(AlchemyTargetAddressDto request);
}
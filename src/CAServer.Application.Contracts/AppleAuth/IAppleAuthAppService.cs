using System.Threading.Tasks;
using CAServer.AppleAuth.Dtos;

namespace CAServer.AppleAuth;

public interface IAppleAuthAppService
{
    Task ReceiveAsync(AppleAuthDto appleAuthDto);
}
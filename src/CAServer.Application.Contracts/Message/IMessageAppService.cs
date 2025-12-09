using System.Threading.Tasks;
using CAServer.Message.Dtos;

namespace CAServer.Message;

public interface IMessageAppService
{
    Task ScanLoginSuccessAsync(ScanLoginDto request);
    Task ScanLoginAsync(ScanLoginDto request);
}
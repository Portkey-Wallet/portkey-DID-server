using System.Threading.Tasks;
using CAServer.Demo.Dtos;

namespace CAServer.Demo;

public interface IDemoAppService
{
    Task<DemoDto> SuccessAsync(DemoRequestDto input);
    Task<DemoDto> ExceptionAsync();
    Task<DemoDto> NotExistErrorAsync();
}
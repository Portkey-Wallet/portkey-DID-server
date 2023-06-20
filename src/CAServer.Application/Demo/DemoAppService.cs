using System;
using System.Threading.Tasks;
using CAServer.Demo.Dtos;
using CAServer.Response;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Demo;

[RemoteService(false), DisableAuditing]
public class DemoAppService : CAServerAppService, IDemoAppService
{
    public Task<DemoDto> SuccessAsync(DemoRequestDto input)
    {
        return Task.FromResult(new DemoDto()
        {
            UserId = Guid.NewGuid(),
            Name = input.Name,
            Age = input.Age
        });
    }

    public Task<DemoDto> ExceptionAsync()
    {
        // null reference
        string str = null;
        int len = str.Length;

        return Task.FromResult(new DemoDto()
        {
            UserId = Guid.NewGuid()
        });
    }

    public Task<DemoDto> NotExistErrorAsync()
    {
        throw new UserFriendlyException(ResponseMessage.NotExist, ResponseCode.NotExist);
    }
}
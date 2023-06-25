using System;
using System.Threading.Tasks;
using CAServer.Demo.Dtos;
using CAServer.Response;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Authorization;
using Volo.Abp.Users;

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

    public Task<DemoDto> UnAuthExceptionAsync()
    {
        var userId = CurrentUser.Id;
        if (userId == null || userId == Guid.Empty)
        {
            throw new AbpAuthorizationException("user id is not set");
        }

        return Task.FromResult(new DemoDto()
        {
            UserId = Guid.NewGuid(),
            Age = 10,
            Name = "John"
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
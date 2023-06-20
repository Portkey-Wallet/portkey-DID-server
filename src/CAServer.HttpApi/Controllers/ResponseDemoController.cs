using System.Threading.Tasks;
using CAServer.Demo;
using CAServer.Demo.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ResponseDemo")]
[Route("api/app/demo")]
[IgnoreAntiforgeryToken]
public class ResponseDemoController : CAServerController
{
    private readonly IDemoAppService _demoAppService;

    public ResponseDemoController(IDemoAppService demoAppService)
    {
        _demoAppService = demoAppService;
    }

    [Authorize]
    [HttpPost("unAuthentication")]
    public Task<DemoDto> UnAuthenticationAsync(DemoRequestDto input)
    {
        return _demoAppService.SuccessAsync(input);
    }

    [Authorize(Roles = "admin")]
    [HttpPost("unAuthorization")]
    public Task<DemoDto> UnAuthorizationAsync(DemoRequestDto input)
    {
        return _demoAppService.SuccessAsync(input);
    }

    [HttpPost("success")]
    public Task<DemoDto> SuccessAsync(DemoRequestDto input)
    {
        return _demoAppService.SuccessAsync(input);
    }

    [HttpPost("exception")]
    public Task<DemoDto> ExceptionAsync()
    {
        return _demoAppService.ExceptionAsync();
    }

    [HttpPost("notExistError")]
    public Task<DemoDto> NotExistErrorAsync()
    {
        return _demoAppService.NotExistErrorAsync();
    }
}
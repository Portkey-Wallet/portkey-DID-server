using System;
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
    
    [HttpPost("unAuthException")]
    public Task<DemoDto> UnAuthExceptionAsync(DemoRequestDto input)
    {
        return _demoAppService.UnAuthExceptionAsync();
    }

    [HttpPost("success")]
    public Task<DemoDto> SuccessAsync(DemoRequestDto input)
    {
        return _demoAppService.SuccessAsync(input);
    }
    
        
    [HttpPost("emptyResult")]
    public Task EmptyResultAsync(DemoRequestDto input)
    {
        return _demoAppService.NoContentAsync();
    }
    
    [HttpPost("noContent")]
    public IActionResult NoContent(DemoRequestDto input)
    {
        return NoContent();
    }
    
    [HttpPost("content")]
    public IActionResult Content()
    {
        return Content("this is from content"); 
    }
    
    [HttpPost("okResult")]
    public IActionResult OkResult()
    {
        return Ok(new { address = "beijing", weather = "sun" });
    }
    
    [HttpPost("okEmptyResult")]
    public IActionResult OkEmptyResult()
    {
        return Ok();
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
    
    [HttpPost("unImplement")]
    public Task<DemoDto> UnImplement()
    {
        throw new NotImplementedException();
    }
}
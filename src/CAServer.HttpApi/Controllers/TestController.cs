using System.Threading.Tasks;
using CAServer.Test;
using CAServer.Test.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Test")]
[Route("api/app/test")]
[IgnoreAntiforgeryToken]
public class TestController : CAServerController
{
    private readonly ITestAppService _testAppService;

    public TestController(ITestAppService testAppService)
    {
        _testAppService = testAppService;
    }

    [HttpPost]
    public async Task<TestResultDto> AddAsync(TestRequestDto input)
    {
        return await _testAppService.AddAsync(input);
    }

    [HttpGet]
    public async Task<TestResultDto> GetAsync(string id)
    {
        return await _testAppService.GetAsync(id);
    }
}
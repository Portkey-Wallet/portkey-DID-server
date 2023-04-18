using System.Threading.Tasks;
using CAServer.Sample;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.Controllers;

[RemoteService]
[ControllerName("Sample")]
[Route("api/app/sample")]
public class SampleController : CAServerController
{
    private readonly ISampleAppService _sampleAppService;

    public SampleController(ISampleAppService sampleAppService)
    {
        _sampleAppService = sampleAppService;
    }
    
    [HttpGet]
    [Route("hello")]
    //[Authorize]
    public virtual Task<string> Hello(string from, string to)
    {
        return _sampleAppService.Hello(from, to);
    }
}
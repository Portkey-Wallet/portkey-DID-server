using CAServer.Growth;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Growth")]
[Route("api/app/growth")]
public class GrowthController : CAServerController
{
    private readonly IGrowthAppService _growthAppService;

    public GrowthController(IGrowthAppService growthAppService)
    {
        _growthAppService = growthAppService;
    }
    
    
    
}
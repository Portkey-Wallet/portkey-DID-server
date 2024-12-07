using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contract")]
[Route("api/app/contracts")]
[IgnoreAntiforgeryToken]
public class ContractController : CAServerController
{
    private readonly IContactAppService _contactAppService;
    private readonly ILogger<ContactController> _logger;
    private readonly ChatBotOptions _botOptions;

    public ContractController(IContactAppService contactAppService, ILogger<ContactController> logger,
        IOptionsSnapshot<ChatBotOptions> botOptions)
    {
        _contactAppService = contactAppService;
        _logger = logger;
        _botOptions = botOptions.Value;
    }

    [HttpGet("websiteValild")]
    public async Task<bool> WebsiteValild(WebsiteInfoParamDto input)
    {
        return await WebsiteInfoHelper.WebsiteAvailable(input);
    }

    [HttpGet("spenderValild")]
    public async Task<bool> SpenderValild(WebsiteInfoParamDto input)
    {
        return await WebsiteInfoHelper.WebsiteAvailable(input);
    }
}
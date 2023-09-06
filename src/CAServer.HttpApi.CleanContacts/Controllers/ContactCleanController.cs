using CAServer.ContactClean;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace CAServer.HttpApi.CleanContacts.Controllers;

[Area("app")]
[ControllerName("ContactClean")]
[Route("api/contactClean/")]
[IgnoreAntiforgeryToken]
public class ContactCleanController : AbpControllerBase
{
    private readonly IContactCleanAppService _contactCleanAppService;

    public ContactCleanController(IContactCleanAppService contactCleanAppService)
    {
        _contactCleanAppService = contactCleanAppService;
    }

    [HttpPost("clean")]
    public async Task<string> ContactCleanAsync(Guid userId)
    {
        return await _contactCleanAppService.ContactCleanAsync(userId);
    }

    [HttpPost("cleanAll")]
    public async Task<int> ContactCleanAllAsync()
    {
        return await _contactCleanAppService.ContactCleanAsync();
    }
}
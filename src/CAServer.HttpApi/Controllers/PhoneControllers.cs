using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.Phone;
using CAServer.Phone.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace CAServer.Controllers;

[Area("app")]
[ControllerName("Phone")]
[Route("api/app/phone/")]
public class PhoneController
{
    private readonly IPhoneAppService _phoneAppService;

    public PhoneController(IPhoneAppService phoneAppService)
    {
        _phoneAppService = phoneAppService;
    }

    [HttpGet("info")]
    public Task<PhoneInfoListDto> PhoneInfoAsync()
    {
        return _phoneAppService.GetPhoneInfoAsync();
    }
}

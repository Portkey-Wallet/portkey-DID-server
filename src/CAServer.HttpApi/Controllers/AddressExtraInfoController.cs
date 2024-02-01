using System.Threading.Tasks;
using CAServer.AddressExtraInfo;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;


[RemoteService]
[Area("app")]
[ControllerName("AddressExtraInfo")]
[Route("api/app/addressInfo/")]
public class AddressExtraInfoController: CAServerController
{
    private readonly IAddressExtraInfoService _service;

    public AddressExtraInfoController(IAddressExtraInfoService service)
    {
        _service = service;
    }

    [HttpGet("get")]
    public async Task<string> GetLoinInAccount()
    {
        return await _service.GetLoinInAccount();
    }
    
}
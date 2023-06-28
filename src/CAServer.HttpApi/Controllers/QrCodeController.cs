using System.Threading.Tasks;
using CAServer.QrCode;
using CAServer.QrCode.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("QrCode")]
[Route("api/app/qrcode")]
public class QrCodeController : CAServerController
{
    private readonly IQrCodeAppService _qrCodeAppService;

    public QrCodeController(IQrCodeAppService qrCodeAppService)
    {
        _qrCodeAppService = qrCodeAppService;
    }

    [HttpPost("exist")]
    public async Task<bool> ExistAsync(QrCodeRequestDto input)
    {
        return await _qrCodeAppService.ExistAsync(input);
    }
}
using System.Threading.Tasks;
using Asp.Versioning;
using CAServer.ImTransfer;
using CAServer.ImTransfer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImTransfer")]
[Route("api/app/imTransfer")]
[IgnoreAntiforgeryToken]
[Authorize]
public class ImTransferController : CAServerController
{
    private readonly IImTransferAppService _imTransferAppService;

    public ImTransferController(IImTransferAppService imTransferAppService)
    {
        _imTransferAppService = imTransferAppService;
    }

    [HttpPost("send")]
    public async Task<ImTransferResponseDto> TransferAsync(ImTransferDto input)
    {
        return await _imTransferAppService.TransferAsync(input);
    }

    [HttpGet("getResult")]
    public async Task<TransferResultDto> GetTransferResultAsync(string transferId)
    {
        return await _imTransferAppService.GetTransferResultAsync(transferId);
    }
}
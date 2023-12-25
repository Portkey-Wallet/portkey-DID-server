using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Signature.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignatureServer.Providers;
using Volo.Abp;

namespace CAServer.Signature;

[RemoteService]
[Route("api/app/signature")]
public class SignatureController : CAServerSignatureController
{
    private readonly ILogger<SignatureController> _logger;
    private readonly AccountProvider _accountProvider;


    public SignatureController(ILogger<SignatureController> logger,
        AccountProvider accountProvider)
    {
        _logger = logger;
        _accountProvider = accountProvider;
    }

    [HttpPost]
    public async Task<SignResponseDto> SendSignAsync(
        SendSignatureDto input)
    {
        try
        {
            _logger.LogDebug("input PublicKey: {PublicKey}, HexMsg: {HexMsg}", input.PublicKey, input.HexMsg);
            var signatureResult = _accountProvider.GetSignature(input.PublicKey, 
                ByteArrayHelper.HexStringToByteArray(input.HexMsg));
            _logger.LogDebug("Signature result :{SignatureResult}", signatureResult);

            return new SignResponseDto
            {
                Signature = signatureResult,
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Signature failed, error msg is {ErrorMsg}", e);
            throw new UserFriendlyException(e.Message);
        }
    }

}
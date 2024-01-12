using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Signature;
using CAServer.Signature.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SignatureServer.Dtos;
using SignatureServer.Providers;
using SignatureServer.Providers.ExecuteStrategy;
using Volo.Abp;

namespace SignatureServer;

[RemoteService]
[Route("api/app/signature")]
public class SignatureController : CAServerSignatureController
{
    private readonly ILogger<SignatureController> _logger;
    private readonly AccountProvider _accountProvider;
    private readonly StorageProvider _storageProvider;
    private readonly AlchemyPayAesSignStrategy _alchemyPayAesSignStrategy;


    public SignatureController(ILogger<SignatureController> logger,
        AccountProvider accountProvider, StorageProvider storageProvider, AlchemyPayAesSignStrategy alchemyPayAesSignStrategy)
    {
        _logger = logger;
        _accountProvider = accountProvider;
        _storageProvider = storageProvider;
        _alchemyPayAesSignStrategy = alchemyPayAesSignStrategy;
    }

    [HttpPost]
    public Task<SignResponseDto> SendSignAsync(
        SendSignatureDto input)
    {
        try
        {
            _logger.LogDebug("input PublicKey: {PublicKey}, HexMsg: {HexMsg}", input.PublicKey, input.HexMsg);
            var signatureResult = _accountProvider.GetSignature(input.PublicKey,
                ByteArrayHelper.HexStringToByteArray(input.HexMsg));
            _logger.LogDebug("Signature result :{SignatureResult}", signatureResult);

            return Task.FromResult(new SignResponseDto
            {
                Signature = signatureResult,
            });
        }
        catch (Exception e)
        {
            _logger.LogError("Signature failed, error msg is {ErrorMsg}", e);
            throw new UserFriendlyException(e.Message);
        }
    }

    [HttpPost("thirdpart/alchemyAes")]
    public Task<SignedResponse<CommonThirdPartExecuteOutput>> AlchemyAesSignAsync(string key)
    {
        var strategyOutput =
            _storageProvider.ExecuteThirdPartSecret(key, new CommonThirdPartExecuteInput(), _alchemyPayAesSignStrategy);
        return Task.FromResult(new SignedResponse<CommonThirdPartExecuteOutput>(strategyOutput, ""));
    }
}
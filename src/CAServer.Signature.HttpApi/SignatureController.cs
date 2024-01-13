using System;
using System.Threading.Tasks;
using AElf;
using CAServer.Signature;
using CAServer.Signature.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignatureServer.Common;
using SignatureServer.Dtos;
using SignatureServer.Options;
using SignatureServer.Providers;
using SignatureServer.Providers.ExecuteStrategy;
using Volo.Abp;

namespace SignatureServer;

[RemoteService]
[Route("api/app")]
public class SignatureController : CAServerSignatureController
{
    private readonly ILogger<SignatureController> _logger;
    private readonly IOptionsMonitor<AuthorityOptions> _authOptions;
    private readonly AccountProvider _accountProvider;
    private readonly StorageProvider _storageProvider;
    private readonly AlchemyPayAesSignStrategy _alchemyPayAesSignStrategy;
    private readonly AlchemyPayShaSignStrategy _alchemyPayShaSignStrategy;
    private readonly AlchemyPayShaSignStrategy _alchemyPayHmacSignStrategy;
    private readonly AppleAuthStrategy _appleAuthStrategy;


    public SignatureController(ILogger<SignatureController> logger,
        AccountProvider accountProvider, StorageProvider storageProvider,
        AlchemyPayAesSignStrategy alchemyPayAesSignStrategy, IOptionsMonitor<AuthorityOptions> authOptions,
        AlchemyPayShaSignStrategy alchemyPayShaSignStrategy, AlchemyPayShaSignStrategy alchemyPayHmacSignStrategy,
        AppleAuthStrategy appleAuthStrategy)
    {
        _logger = logger;
        _accountProvider = accountProvider;
        _storageProvider = storageProvider;
        _alchemyPayAesSignStrategy = alchemyPayAesSignStrategy;
        _authOptions = authOptions;
        _alchemyPayShaSignStrategy = alchemyPayShaSignStrategy;
        _alchemyPayHmacSignStrategy = alchemyPayHmacSignStrategy;
        _appleAuthStrategy = appleAuthStrategy;
    }

    [HttpPost("signature")]
    public Task<SignResponseDto> SendSignAsync(SendSignatureDto input)
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

    [HttpGet("secret")]
    public Task<CommonResponse<string>> GetSecret(string key)
    {
        var (_, appsecret) = AuthorityHelper.AssertDappHeader(_authOptions.CurrentValue, HttpContext, key);
        var secret = _storageProvider.GetThirdPartSecret(key);
        secret = EncryptHelper.AesCbcEncrypt(secret, appsecret);
        return Task.FromResult(new CommonResponse<string>(secret));
    }

    [HttpPost("thirdPart/alchemyAes")]
    public Task<CommonResponse<CommonThirdPartExecuteOutput>> AlchemyAesSignAsync(
        CommonThirdPartExecuteInput input)
    {
        var (_, appsecret) = AuthorityHelper.AssertDappHeader(_authOptions.CurrentValue, HttpContext,
            input.Key, input.BizData);
        var strategyOutput = _storageProvider.ExecuteThirdPartSecret(input, _alchemyPayAesSignStrategy);
        // strategyOutput.Value = EncryptHelper.AesCbcEncrypt(strategyOutput.Value, appsecret);
        return Task.FromResult(new CommonResponse<CommonThirdPartExecuteOutput>(strategyOutput));
    }
    
    [HttpPost("thirdPart/alchemySha")]
    public Task<CommonResponse<CommonThirdPartExecuteOutput>> AlchemyShaSignAsync(
        CommonThirdPartExecuteInput input)
    {
        var (_, appsecret) = AuthorityHelper.AssertDappHeader(_authOptions.CurrentValue, HttpContext,
            input.Key, input.BizData);
        var strategyOutput = _storageProvider.ExecuteThirdPartSecret(input, _alchemyPayShaSignStrategy);
        // strategyOutput.Value = EncryptHelper.AesCbcEncrypt(strategyOutput.Value, appsecret);
        return Task.FromResult(new CommonResponse<CommonThirdPartExecuteOutput>(strategyOutput));
    }
    
    [HttpPost("thirdPart/alchemyHmac")]
    public Task<CommonResponse<CommonThirdPartExecuteOutput>> AlchemyHmacSignAsync(
        CommonThirdPartExecuteInput input)
    {
        var (_, appsecret) = AuthorityHelper.AssertDappHeader(_authOptions.CurrentValue, HttpContext,
            input.Key, input.BizData);
        var strategyOutput = _storageProvider.ExecuteThirdPartSecret(input, _alchemyPayHmacSignStrategy);
        // strategyOutput.Value = EncryptHelper.AesCbcEncrypt(strategyOutput.Value, appsecret);
        return Task.FromResult(new CommonResponse<CommonThirdPartExecuteOutput>(strategyOutput));
    }
    
    [HttpPost("thirdPart/appleAuth")]
    public Task<CommonResponse<CommonThirdPartExecuteOutput>> AlchemyHmacSignAsync(
        AppleAuthExecuteInput input)
    {
        var (_, appsecret) = AuthorityHelper.AssertDappHeader(_authOptions.CurrentValue, HttpContext,
            input.Key, input.KeyId, input.TeamId, input.ClientId);
        var strategyOutput = _storageProvider.ExecuteThirdPartSecret(input, _appleAuthStrategy);
        // strategyOutput.Value = EncryptHelper.AesCbcEncrypt(strategyOutput.Value, appsecret);
        return Task.FromResult(new CommonResponse<CommonThirdPartExecuteOutput>(strategyOutput));
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using CAServer.Signature.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace CAServer.Signature;

[RemoteService]
[Route("api/app/signature")]
public class SignatureController : CAServerSignatureController
{
    private readonly ILogger<SignatureController> _logger;
    private readonly SignatureOptions _signatureOptions;


    public SignatureController(ILogger<SignatureController> logger,
        IOptions<SignatureOptions> signatureOptions)
    {
        _logger = logger;
        _signatureOptions = signatureOptions.Value;
    }

    [HttpPost]
    public async Task<SignatureResponseDto> SendSignatureAsync(
        SendSignatureDto input)
    {
        try
        {
            var privateKey = GetPrivateKeyByPublishKey(input.PublicKey);
            var msgHashBytes = ByteStringHelper.FromHexString(input.HexMsg);
            var msgHashByteStr = msgHashBytes.ToString();
            var recoverableInfo = CryptoHelper.SignWithPrivateKey(privateKey, msgHashBytes.ToByteArray());
            return new SignatureResponseDto
            {
                Signature = recoverableInfo.ToHex(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Signature failed, error msg is {errorMsg}", e);
            throw new UserFriendlyException(e.Message);
        }
    }

    private byte[] GetPrivateKeyByPublishKey(string publishKey)
    {
        if (_signatureOptions.PrivateKeyDictionary.TryGetValue(publishKey, out string _))
        {
            return Encoding.UTF8.GetBytes(_signatureOptions.PrivateKeyDictionary[publishKey]);
        }

        _logger.LogError("Publish key {publishKey} not exist!", publishKey);
        throw new KeyNotFoundException("Publish key not exist!");
    }
}
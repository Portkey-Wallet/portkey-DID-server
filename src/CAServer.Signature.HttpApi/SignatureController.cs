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
    private readonly KeyPairInfoOptions _keyPairInfoOptions;


    public SignatureController(ILogger<SignatureController> logger,
        IOptionsSnapshot<KeyPairInfoOptions> signatureOptions)
    {
        _logger = logger;
        _keyPairInfoOptions = signatureOptions.Value;
    }

    [HttpPost]
    public async Task<SignResponseDto> SendSignAsync(
        SendSignatureDto input)
    {
        try
        {
            _logger.LogDebug("input PublicKey: {PublicKey}, HexMsg: {HexMsg}", input.PublicKey, input.HexMsg);
            var privateKey = GetPrivateKeyByPublicKey(input.PublicKey);
            var msgHashBytes = ByteStringHelper.FromHexString(input.HexMsg);
            var recoverableInfo = CryptoHelper.SignWithPrivateKey(privateKey, msgHashBytes.ToByteArray());
            _logger.LogDebug("Signature result :{signatureResult}", recoverableInfo.ToHex());

            return new SignResponseDto
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

    private byte[] GetPrivateKeyByPublicKey(string publicKey)
    {
        if (_keyPairInfoOptions.PrivateKeyDictionary.TryGetValue(publicKey, out string _))
        {
            return Encoding.UTF8.GetBytes(_keyPairInfoOptions.PrivateKeyDictionary[publicKey]);
        }

        _logger.LogError("Publish key {publishKey} not exist!", publicKey);
        throw new KeyNotFoundException("Publish key not exist!");
    }
}
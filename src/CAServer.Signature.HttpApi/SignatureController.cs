using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using CAServer.Signature.Dtos;
using Google.Protobuf;
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
    private readonly KeyStoreOptions _keyStoreOptions;
    private readonly IAElfKeyStoreService _aelfKeyStoreService;

    public SignatureController(ILogger<SignatureController> logger,
        IOptionsSnapshot<KeyPairInfoOptions> signatureOptions,
        IOptionsSnapshot<KeyStoreOptions> keyStoreOptions,
        IAElfKeyStoreService aelfKeyStoreService)
    {
        _logger = logger;
        _keyPairInfoOptions = signatureOptions.Value;
        _keyStoreOptions = keyStoreOptions.Value;
        _aelfKeyStoreService = aelfKeyStoreService;
    }

    [HttpPost]
    public async Task<SignResponseDto> SendSignAsync(
        SendSignatureDto input)
    {
        try
        {
            _logger.LogDebug("input PublicKey: {PublicKey}, HexMsg: {HexMsg}", input.PublicKey, input.HexMsg);
            // var privateKey = GetPrivateKeyByPublicKey(input.PublicKey);
            var privateKey = GetPrivateKeyByKeyStore();
            var recoverableInfo = CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey),
                ByteArrayHelper.HexStringToByteArray(input.HexMsg));
            _logger.LogDebug("Signature result :{signatureResult}", recoverableInfo.ToHex());

            return new SignResponseDto
            {
                Signature = ByteString.CopyFrom(recoverableInfo).ToHex(),
            };
        }
        catch (Exception e)
        {
            _logger.LogError("Signature failed, error msg is {errorMsg}", e);
            throw new UserFriendlyException(e.Message);
        }
    }

    private string GetPrivateKeyByPublicKey(string publicKey)
    {
        if (_keyPairInfoOptions.PrivateKeyDictionary.TryGetValue(publicKey, out string _))
        {
            return _keyPairInfoOptions.PrivateKeyDictionary[publicKey];
        }

        _logger.LogError("Publish key {publishKey} not exist!", publicKey);
        throw new KeyNotFoundException("Publish key not exist!");
    }
    
    private string GetPrivateKeyByKeyStore()
    {
        if (System.IO.File.Exists(_keyStoreOptions.KeyStorePath))
        {
            return _aelfKeyStoreService.DecryptKeyStore(_keyStoreOptions).ToHex();
        }

        _logger.LogError("KeyStore file not exist!", _keyStoreOptions.KeyStorePath);
        throw new KeyNotFoundException("KeyStore file not exist!");
    }
}
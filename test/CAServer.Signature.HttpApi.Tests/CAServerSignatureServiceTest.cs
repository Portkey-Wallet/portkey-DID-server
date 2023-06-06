using System.Threading.Tasks;
using CAServer.Signature.Dtos;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Signature.Test;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class CAServerSignatureServiceTest : CAServerSignatureHttpApiTestBase
{
    private readonly SignatureController _signatureController;

    public CAServerSignatureServiceTest()
    {
        _signatureController = GetRequiredService<SignatureController>();
    }


    [Fact]
    public async Task GetSignatureByPublicKeySucceedAsyncTest()
    {
        var result = await _signatureController.SendSignAsync(new SendSignatureDto()
        {
            HexMsg = TestHexMsgGenerator(),
            PublicKey = "test-key"
        });
        result.Signature.ShouldBe(
            "d71f1ada7df0aa23f2cfe27234f63c6811ebd39d6b5a56878ec366abf94b73a63c9e56d2f1c4608047325a2c1c859536beaeb70fb4736476583552197e86a41900");
    }

    [Fact]
    public async Task GetSignatureByPublicKeyNotExistAsyncTest()
    {
        try
        {
            var result = await _signatureController.SendSignAsync(new SendSignatureDto()
            {
                HexMsg = TestHexMsgGenerator("Public key not exist."),
                PublicKey = "not-exist-key"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Publish key not exist!");
        }
    }

    private static string TestHexMsgGenerator(string message = "CAServer.Signature.TestString")
    {
        byte[] hashBytes = SHA256.Create()
            .ComputeHash(Encoding.UTF8.GetBytes(message));

        return hashBytes.ToHex();
    }
}
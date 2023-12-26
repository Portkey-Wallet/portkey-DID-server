using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AElf;
using CAServer.Signature.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using SignatureServer.Options;
using Xunit;

namespace CAServer.Signature.Test;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class CaServerSignatureServiceTest : CAServerSignatureHttpApiTestBase
{
    private readonly SignatureController _signatureController;

    public CaServerSignatureServiceTest()
    {
        _signatureController = GetRequiredService<SignatureController>();
    }


    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockKeyStoreOptions());
        services.AddSingleton(MockKeyPairStoreOptions());
    }

    private IOptions<KeyStoreOptions> MockKeyStoreOptions()
    {
        var option = new KeyStoreOptions()
        {
            Path = "/",
            Passwords = new(),
            LoadAddress = new()
        };
        var mock = new Mock<IOptions<KeyStoreOptions>>();
        mock.Setup(p => p.Value).Returns(option);
        return mock.Object;
    }

    private IOptions<KeyPairInfoOptions> MockKeyPairStoreOptions()
    {
        var option = new KeyPairInfoOptions()
        {
            PrivateKeyDictionary = new Dictionary<string, string>
            {
                ["254e2J2NmCU1uevrJVpErYXGTUvbeGVs1NmL6eDssL9WGwzM9F"] = "b3fd383e6c8874721f5e23e475ea3a80e6602d383f92269de2f7dbc76b3b5e9e"
            }
        };
        var mock = new Mock<IOptions<KeyPairInfoOptions>>();
        mock.Setup(p => p.Value).Returns(option);
        return mock.Object;
    }

    [Fact]
    public async Task GetSignatureByPublicKeySucceedAsyncTest()
    {
        var result = await _signatureController.SendSignAsync(new SendSignatureDto()
        {
            HexMsg = TestHexMsgGenerator(),
            PublicKey = "254e2J2NmCU1uevrJVpErYXGTUvbeGVs1NmL6eDssL9WGwzM9F"
        });
        result.Signature.ShouldNotBe("");
    }

    [Fact]
    public async Task GetSignatureByPublicKeyNotExistAsyncTest()
    {
        try
        {
            var result = await _signatureController.SendSignAsync(new SendSignatureDto()
            {
                HexMsg = TestHexMsgGenerator("Account not found."),
                PublicKey = "not-exist-key"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldContain("Account not found");
        }
    }

    private static string TestHexMsgGenerator(string message = "CAServer.Signature.TestString")
    {
        byte[] hashBytes = SHA256.Create()
            .ComputeHash(Encoding.UTF8.GetBytes(message));

        return hashBytes.ToHex();
    }
}
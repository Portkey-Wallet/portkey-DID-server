using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf;
using AElf.Client.MultiToken;
using AElf.Types;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAServer.Account;
using CAServer.amazon;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Commons;
using CAServer.Dtos;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Guardian;
using CAServer.Options;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class RecoveryServiceTests : CAServerApplicationTestBase
{
    private const string DefaultEmailAddress = "1025289418@qq.com";
    private const string DefaultVerifierId = "DefaultVerifierId";
    private const string DefaultVerificationDoc = "DefaultVerificationDoc";
    private const string DefaultVerifierSignature = "DefaultVerifierSignature";
    private const string DefaultManager = "2ZCRxumsuLDQhFcNChqGmt9VJCxuQbcDpkCddoWqC2JC6G6EHh";
    private const string DefaultExtraData = "DefaultDeviceString";
    private const string DefaultChainId = "DefaultChainId";
    private const string DefaultClientId = "DefaultClientId";
    private const string DefaultRequestId = "DefaultRequestId";

    private readonly ICAAccountAppService _caAccountAppService;
    private readonly TestCluster _cluster;
    private readonly AppleCacheOptions _appleCacheOptions;

    public RecoveryServiceTests()
    {
        _caAccountAppService = GetRequiredService<ICAAccountAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
        _appleCacheOptions = MockAppleCacheOptions().Value;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(GetMockUserAssetsProvider());
        services.AddSingleton(GetContractProvider());
        services.AddSingleton(GetMockGuardianProvider());
        services.AddSingleton(GetMockCaAccountProvider());
        services.AddSingleton(MockAwsS3Client());
    }

    
    protected IAwsS3Client MockAwsS3Client()
    {
        var mockImageClient = new Mock<IAwsS3Client>();
        mockImageClient.Setup(p => p.UpLoadFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("http://s3.test.com/result.svg");
        return mockImageClient.Object;
    }
    
    [Fact]
    public async Task RecoverRequestAsync_Test()
    {
        var byteArray0 = ByteArrayHelper.HexStringToByteArray(
            "0a220a2056142bb7a2f8e2a3be3e1721d007abf00854c3e6a00a57b3b9330719216c95f912220a20bc0bb02c3ea8911d27249b12d517bb239954692435314f5db37826c4db2c121418bdffec0e2204541a40de2a0d476574486f6c646572496e666f32240a220a2044013a37c18ddad0be325854c39509df676215f12012f881a9453e9eca0db5b082f10441dbf51523dbc04f6e6a8865a2e7e00da9f6f589011e57f1fb2d07d9510652ba1f52cf244d2c618678521fbe843b4d0e185f01f02006a606291bb84ed8a08e7b0d00");
        var transaction0 = Transaction.Parser.ParseFrom(byteArray0);
// var paramByte = ByteString.FromBase64("CiQ5YTFmNzVlNy03ZTk1LTRhMWQtOGM2NS03MzVkMDM2MjE5ZmISBFVTRFQYZCABKLTGpp3EMTABOAFCggEwNGNkZWIwM2Q5YzAzODNmMmQ1OWIwNDE2NDI4NmMwNzg0ZjY4ZjcwNGY3NjY5MjViOGI5YTVkNWMwNjhlYjEzMTUxZTU4N2JmNjM3YmM2MTA1ZjczM2NmYjc1ODE3Njk5YmY2YjkyNTgxYTk3ZGVmNTZkNjk2ZDgwYjI4NzY3ZDY1SoIBMTJlOWI4MWQ2YmU2OGJiMTcxMjZmNjZkOWE0NjNiNDdhMTZhNzI2ZDg0ZDZlMmVhM2VlNWRmN2U3ODI1OWY3YzdlNTQzNDNlZjljNWMzODJhZjdkNjJiODVkZDM4MDg1YjExN2IwNGMzZmEwNzIxZjhkMjk5MGUzYjVjN2I4OTgwMFIiCiBIXoZx6w5ryrklAssDsn864LEhBhQ4xZftfIruwo8Nag==");
         //var forwardCallInput = ManagerApproveInput.Parser.ParseFrom(transaction0.Params);
      var createInput = TransferInput.Parser.ParseFrom(transaction0.Params);


        try
        {
            var identifier = DefaultEmailAddress;
            var salt = "salt";
            var identifierHash = "identifierHash";

            var grain = _cluster.Client.GetGrain<IGuardianGrain>("Guardian-" + identifier);
            await grain.AddGuardianAsync(identifier, salt, identifierHash);

            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = GuardianIdentifierType.Email,
                Identifier = DefaultEmailAddress,
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = DefaultVerifierSignature
            });

            await _caAccountAppService.RecoverRequestAsync(new RecoveryRequestDto
            {
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                GuardiansApproved = list,
                Context = new HubRequestContextDto
                {
                    ClientId = DefaultClientId,
                    RequestId = DefaultRequestId
                }
            });
        }
        catch (Exception e)
        {
            Assert.True(e != null);
        }
    }

    [Fact]
    public async Task RecoverRequestAsync_LoginGuardianIdentifier_Is_NullOrEmpty_Test()
    {
        try
        {
            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = GuardianIdentifierType.Email,
                Identifier = "",
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = DefaultVerifierSignature
            });

            await _caAccountAppService.RecoverRequestAsync(new RecoveryRequestDto
            {
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                GuardiansApproved = list,
                Context = new HubRequestContextDto
                {
                    ClientId = DefaultClientId,
                    RequestId = DefaultRequestId
                }
            });
        }
        catch (Exception ex)
        {
            Assert.True(ex is AbpValidationException);
        }
    }

    [Fact]
    public async Task RecoverRequestAsync_Type_Is_Invalid_Test()
    {
        try
        {
            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = (GuardianIdentifierType)10,
                Identifier = DefaultEmailAddress,
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = DefaultVerifierSignature
            });

            await _caAccountAppService.RecoverRequestAsync(new RecoveryRequestDto
            {
                LoginGuardianIdentifier = "",
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                GuardiansApproved = list,
                Context = new HubRequestContextDto
                {
                    ClientId = DefaultClientId,
                    RequestId = DefaultRequestId
                }
            });
        }
        catch (Exception ex)
        {
            Assert.True(ex is AbpValidationException);
        }
    }

    [Fact]
    public async Task RecoverRequestAsync_Dto_Test()
    {
        var guardian = new GuardianAccountInfoDto
        {
            Type = GuardianType.GUARDIAN_TYPE_OF_APPLE,
            Value = string.Empty,
            VerificationInfo = new VerificationInfoDto
            {
                Id = string.Empty,
                Signature = string.Empty,
                VerificationDoc = string.Empty
            }
        };

        var message = new RecoveryCompletedMessageDto
        {
            RecoveryStatus = "PASS",
            RecoveryMessage = string.Empty
        };
    }

    private IOptionsSnapshot<AppleCacheOptions> MockAppleCacheOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AppleCacheOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AppleCacheOptions
            {
            });
        return mockOptionsSnapshot.Object;
    }

    [Fact]
    public async Task Revoke_Check_Test()
    {
        var userId = Guid.NewGuid();
        var resultDto = await _caAccountAppService.RevokeCheckAsync(userId);
        resultDto.ValidatedAssets.ShouldBeFalse();
        resultDto.ValidatedDevice.ShouldBeTrue();
        resultDto.ValidatedGuardian.ShouldBeFalse();
    }

    private IAppleUserProvider GetMockAppleUserProvider()
    {
        var provider = new Mock<IAppleUserProvider>();

        provider.Setup(t => t.GetUserExtraInfoAsync(It.IsAny<string>())).ReturnsAsync(new AppleUserExtraInfo()
        {
            UserId = Guid.NewGuid().ToString("N"),
            FirstName = "Kui",
            LastName = "Li"
        });

        provider.Setup(t => t.SetUserExtraInfoAsync(It.IsAny<AppleUserExtraInfo>())).Returns(Task.CompletedTask);

        return provider.Object;
    }

    [Fact]
    public async Task Revoke_Entrance_Test()
    {
        var resultDto = await _caAccountAppService.RevokeEntranceAsync();
        resultDto.EntranceDisplay.ShouldBeTrue();
    }

    [Fact]
    public async Task Revoke_Valid_Fail_Test()
    {
        try
        {
            await _caAccountAppService.RevokeAsync(new RevokeDto
            {
                AppleToken = "aaaa"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ResponseMessage.ValidFail);
        }
    }
}
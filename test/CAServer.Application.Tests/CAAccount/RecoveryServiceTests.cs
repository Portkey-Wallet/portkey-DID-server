using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAServer.Account;
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
    }

    [Fact]
    public async Task RecoverRequestAsync_Test()
    {
        
        
        var privateKey = "36bc3f264aa340d44aada5759a5a86aac6d734f19932397e551d9e69edffe0d2";
        var delegateInfo = new Portkey.Contracts.CA.DelegateInfo
        {
            IdentifierHash = Hash.LoadFromHex("a040aeb03991af74ab177243b6b55db5bedf55e4fa7ea3fefa46df12820f5fc2"),
            ChainId = 9992731,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(1706189933).ToTimestamp(),
            ExpirationTime = 3600,
            //ProjectHash = Hash.LoadFromHex("dc168643a3378c9d89cb0f53474b97a292f324b621edea078653721dfa26bb42"),
            ProjectHash = Hash.LoadFromHex("b144905e8a38027de8a059b67570653a0fca8e5841fab8ecb0580f20617ab836"),
            Delegations =
            {
                new Dictionary<string, long>
                {
                    ["ELF"] = 10000000000
                }
            },
            IsUnlimitedDelegate = false,
            Signature = ""
        };
        var signature = CryptoHelper.SignWithPrivateKey(ByteArrayHelper.HexStringToByteArray(privateKey), HashHelper.ComputeFrom(delegateInfo).ToByteArray()).ToHex();



        
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
        var resultDto =  await _caAccountAppService.RevokeCheckAsync(userId);
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
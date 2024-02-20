using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Cryptography;
using AElf.Types;
using CAServer.Account;
using CAServer.amazon;
using CAServer.AppleAuth.Provider;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Guardian;
using CAServer.Options;
using Google.Protobuf.Collections;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class RegisterServiceTests : CAServerApplicationTestBase
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

    public RegisterServiceTests()
    {
        _caAccountAppService = GetRequiredService<ICAAccountAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockAppleUserProvider());
        services.AddSingleton(MockAwsS3Client());
        services.AddSingleton(MockGraphQlOptions());
    }
    
    protected IAwsS3Client MockAwsS3Client()
    {
        var mockImageClient = new Mock<IAwsS3Client>();
        mockImageClient.Setup(p => p.UpLoadFileAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("http://s3.test.com/result.svg");
        return mockImageClient.Object;
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

    private IGraphQLProvider MockGraphQLProvider()
    {

        var mock = new Mock<IGraphQLProvider>();
        // mock.Setup()
        return mock.Object;
    }
    
    
    protected new IOptionsSnapshot<GraphQLOptions> MockGraphQlOptions()
    {
        var options = new GraphQLOptions()
        {
            Configuration = "http://127.0.0.1:9200/AElfIndexer_DApp/PortKeyIndexerCASchema/graphq"
        };

        var mock = new Mock<IOptionsSnapshot<GraphQLOptions>>();
        mock.Setup(o => o.Value).Returns(options);
        return mock.Object;
    }

    

    [Fact]
    public async Task RegisterRequestAsync_Register_Success_Test()
    {
        var identifier = DefaultEmailAddress;
        var salt = "salt";
        var identifierHash = "identifierHash";

        var grain = _cluster.Client.GetGrain<IGuardianGrain>("Guardian-" + identifier);
        await grain.AddGuardianAsync(identifier, salt, identifierHash);

        var result = await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
        {
            Type = GuardianIdentifierType.Email,
            LoginGuardianIdentifier = DefaultEmailAddress,
            Manager = DefaultManager,
            ExtraData = DefaultExtraData,
            ChainId = DefaultChainId,
            VerifierId = DefaultVerifierId,
            VerificationDoc = DefaultVerificationDoc,
            Signature = DefaultVerifierSignature,
            Context = new HubRequestContextDto
            {
                ClientId = DefaultClientId,
                RequestId = DefaultRequestId
            }
        });

        result.ShouldNotBeNull();
        result.SessionId.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RegisterRequestAsync_Type_Not_Exist_Test()
    {
        try
        {
            var result = await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = (GuardianIdentifierType)8,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = DefaultVerifierSignature,
                Context = new HubRequestContextDto
                {
                    ClientId = DefaultClientId,
                    RequestId = DefaultRequestId
                }
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task RegisterRequestAsync_Register_LoginGuardianIdentifier_Is_NullOrEmpty_Test()
    {
        try
        {
            var message = new AccountCompletedMessageBase
            {
                CaAddress = string.Empty,
                CaHash = string.Empty
            };

            var header = new ActivityHeader
            {
                PubKey = string.Empty
            };

            var info = new GuardianAccountInfoDto
            {
                Type = GuardianType.GUARDIAN_TYPE_OF_APPLE,
                Value = string.Empty,
                VerificationInfo = new VerificationInfoDto
                {
                    VerificationDoc = string.Empty,
                    Id = string.Empty,
                    Signature = string.Empty
                }
            };

            var registerMessage = new RegisterCompletedMessageDto
            {
                RegisterStatus = "PASS",
                RegisterMessage = string.Empty
            };

            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = GuardianIdentifierType.Email,
                LoginGuardianIdentifier = "",
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = DefaultVerifierSignature,
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

    private IOptionsSnapshot<AppleCacheOptions> MockAppleCacheOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<AppleCacheOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new AppleCacheOptions
            {
            });
        return mockOptionsSnapshot.Object;
    }
}
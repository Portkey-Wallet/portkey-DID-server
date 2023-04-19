using System;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Guardian;
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
        _caAccountAppService = GetService<ICAAccountAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
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
    public async Task RegisterRequestAsync_Register_LoginGuardianIdentifier_Is_NullOrEmpty_Test()
    {
        try
        {
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
}
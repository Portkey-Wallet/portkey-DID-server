using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using CAServer.Grains.Grain.Guardian;
using Orleans.TestingHost;
using Volo.Abp.Validation;
using Xunit;

namespace CAServer.CAAccount;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class RecoveryServiceTests : CAServerApplicationTestBase
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

    public RecoveryServiceTests()
    {
        _caAccountAppService = GetService<ICAAccountAppService>();
        _cluster = Cluster;
    }

    [Fact]
    public async Task RecoverRequestAsync_Test()
    {
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
    
}
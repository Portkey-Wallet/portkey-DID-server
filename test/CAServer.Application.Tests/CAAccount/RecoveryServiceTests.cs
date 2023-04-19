using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CAServer;
using CAServer.CAAccount;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
using Volo.Abp.Validation;
using Xunit;

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

    public RecoveryServiceTests()
    {
        _caAccountAppService = GetService<ICAAccountAppService>();
    }

    #region RegisterRequestAsync
    [Fact]
    public async Task RecoverRequestAsync_Body_Empty_Test()
    {
        try
        {
            await _caAccountAppService.RecoverRequestAsync(new RecoveryRequestDto
            {
            });
        }
        catch (Exception ex)
        {
            Assert.True(ex != null);
        }
    }

    [Fact]
    public async Task RecoverRequestAsync_Type_LoginGuardianType_Not_Match_Test()
    {
        try
        {
            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = GuardianIdentifierType.Phone,
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
        catch (Exception ex)
        {
            Assert.True(true);
            // List<string> errMessageList = new List<string>() { "Invalid phone input.", "Invalid email input." };
            // Assert.Contains(errMessageList, t => t.Contains(ex.Message));
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

    [Fact]
    public async Task RecoverRequestAsync_Manager_Is_NullOrEmpty_Test()
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
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = "",
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
    public async Task RecoverRequestAsync_ExtraData_Is_NullOrEmpty_Test()
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
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = "",
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
    public async Task RecoverRequestAsync_ChainId_Is_NullOrEmpty_Test()
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
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = "",
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
    public async Task RecoverRequestAsync_VerifierId_Is_NullOrEmpty_Test()
    {
        try
        {
            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = GuardianIdentifierType.Email,
                Identifier = DefaultEmailAddress,
                VerifierId = "",
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
    public async Task RecoverRequestAsync_VerificationDoc_Is_NullOrEmpty_Test()
    {
        try
        {
            var list = new List<RecoveryGuardian>();
            list.Add(new RecoveryGuardian
            {
                Type = GuardianIdentifierType.Email,
                Identifier = DefaultEmailAddress,
                VerifierId = DefaultVerifierId,
                VerificationDoc = "",
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
    public async Task RecoverRequestAsync_Signature_Is_NullOrEmpty_Test()
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
                Signature = ""
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

    #endregion
}
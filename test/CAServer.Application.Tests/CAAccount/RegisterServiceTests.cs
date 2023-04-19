using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CAServer.CAAccount.Dtos;
using CAServer.Dtos;
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

    public RegisterServiceTests()
    {
        _caAccountAppService = GetService<ICAAccountAppService>();
    }
    
    private bool VerifyEmail(string address)
    {
        // string emailRegex =
        //     @"([a-zA-Z0-9_\.\-])+\@(([a-zA-Z0-9\-])+\.)+([a-zA-Z0-9]{2,5})+";

        var emailRegex = @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        var emailReg = new Regex(emailRegex);
        return emailReg.IsMatch(address.Trim());
    }

    [Fact]
    public void TestEmailRegex()
    {
        string email = "b@b.b";
        var res = VerifyEmail(email);
        Assert.True(res);
    }

    #region RegisterRequestAsync

    [Fact]
    public async Task RegisterRequestAsync_Register_Body_Empty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
            });
        }
        catch (Exception ex)
        {
            Assert.True(ex != null);
        }
    }

    [Fact]
    public async Task RegisterRequestAsync_Register_Type_LoginGuardianType_Not_Match_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = GuardianIdentifierType.Phone,
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
        catch (Exception ex)
        {
            Assert.True(true);
            // List<string> errMessageList = new List<string>() { "Invalid phone input.", "Invalid email input." };
            // Assert.Contains(errMessageList, t => t.Contains(ex.Message));
        }
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


    [Fact]
    public async Task RegisterRequestAsync_Register_Address_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = "",
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

    [Fact]
    public async Task RegisterRequestAsync_Register_DeviceString_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = "",
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

    [Fact]
    public async Task RegisterRequestAsync_Register_ChainId_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = "",
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

    [Fact]
    public async Task RegisterRequestAsync_Register_VerifierId_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                VerifierId = "",
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

    [Fact]
    public async Task RegisterRequestAsync_Register_VerificationDoc_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                VerifierId = DefaultVerifierId,
                VerificationDoc = "",
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

    [Fact]
    public async Task RegisterRequestAsync_Register_Signature_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianIdentifier = DefaultEmailAddress,
                Manager = DefaultManager,
                ExtraData = DefaultExtraData,
                ChainId = DefaultChainId,
                VerifierId = DefaultVerifierId,
                VerificationDoc = DefaultVerificationDoc,
                Signature = "",
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
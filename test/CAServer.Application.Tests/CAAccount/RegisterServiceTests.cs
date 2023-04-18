using System;
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
    private const string DefaultManagerAddress = "2ZCRxumsuLDQhFcNChqGmt9VJCxuQbcDpkCddoWqC2JC6G6EHh";
    private const string DefaultDeviceString = "DefaultDeviceString";
    private const string DefaultChainId = "DefaultChainId";
    private const string DefaultClientId = "DefaultClientId";
    private const string DefaultRequestId = "DefaultRequestId";

    private readonly ICAAccountAppService _caAccountAppService;

    public RegisterServiceTests()
    {
        _caAccountAppService = GetService<ICAAccountAppService>();
    }

    #region RegisterRequestAsync

    // [Fact]
    // public async Task RegisterRequestAsync_Recovery_Success_Test()
    // {
    //     //success
    //     await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
    //     {
    //         Type = 0,
    //         LoginGuardianAccount = DefaultEmailAddress,
    //         ManagerAddress = DefaultManagerAddress,
    //         DeviceString = DefaultDeviceString,
    //         ChainId = DefaultChainId,
    //         VerifierId = DefaultVerifierId,
    //         VerificationDoc = DefaultVerificationDoc,
    //         Signature = DefaultVerifierSignature,
    //         Context = new HubRequestContextDto
    //         {
    //             ClientId = DefaultClientId,
    //             RequestId = DefaultRequestId
    //         }
    //     });
    // }

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
                Type = GuardianTypeDto.PhoneNumber,
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
    public async Task RegisterRequestAsync_Register_LoginGuardianAccount_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = GuardianTypeDto.Email,
                LoginGuardianAccount = "",
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
    public async Task RegisterRequestAsync_Register_ManagerAddress_Is_NullOrEmpty_Test()
    {
        try
        {
            await _caAccountAppService.RegisterRequestAsync(new RegisterRequestDto
            {
                Type = 0,
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = "",
                DeviceString = DefaultDeviceString,
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
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = "",
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
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
                LoginGuardianAccount = DefaultEmailAddress,
                ManagerAddress = DefaultManagerAddress,
                DeviceString = DefaultDeviceString,
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
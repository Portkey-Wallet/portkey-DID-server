using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using CAServer.AppleVerify;
using CAServer.Cache;
using CAServer.Common;
using CAServer.Dtos;
using CAServer.Hub;
using CAServer.Verifier;
using CAServer.Verifier.Dtos;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace CAServer.VerifierCode;

public partial class VerifierCodeTests
{
    private IVerifierServerClient GetMockVerifierServerClient()
    {
        var mockVerifierServerClient = new Mock<IVerifierServerClient>();
        mockVerifierServerClient.Setup(o => o.SendVerificationRequestAsync(It.IsAny<VerifierCodeRequestDto>()))
            .ReturnsAsync((VerifierCodeRequestDto dto) =>
            {
                if (dto.Type == EmailType)
                {
                    return new ResponseResultDto<VerifierServerResponse>
                    {
                        Success = true,
                        Data = new VerifierServerResponse
                        {
                            VerifierSessionId = Guid.NewGuid()
                        }
                    };
                }

                return new ResponseResultDto<VerifierServerResponse>
                {
                    Success = false,
                    Message = "Send VerifierCode Failed."
                };
            });
        mockVerifierServerClient.Setup(o => o.VerifyCodeAsync(It.IsAny<VierifierCodeRequestInput>()))
            .ReturnsAsync((VierifierCodeRequestInput dto) =>
            {
                if (dto.ChainId == DefaultChainId)
                {
                    return new ResponseResultDto<VerificationCodeResponse>
                    {
                        Success = true,
                        Data = new VerificationCodeResponse
                        {
                            Signature = "signature",
                            VerificationDoc = "verificationDoc"
                        }
                    };
                }

                return new ResponseResultDto<VerificationCodeResponse>
                {
                    Success = false,
                    Message = "Verify VerifierCode Failed."
                };
            });

        mockVerifierServerClient.Setup(o =>
                o.VerifyAppleTokenAsync(It.IsAny<VerifyTokenRequestDto>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ResponseResultDto<VerifyAppleTokenDto>
            {
                Success = true,
                Data = new VerifyAppleTokenDto
                {
                    AppleUserExtraInfo = new AppleUserExtraInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        AuthTime = new DateTime(),
                        Email = "",
                        GuardianType = "Email",
                        IsPrivateEmail = true,
                        VerifiedEmail = true
                    },
                    Signature = "MockAppleSignature",
                    VerificationDoc = "MockAppleVerificationDoc"
                },
            });
        
        mockVerifierServerClient.Setup(o =>
                o.VerifyGoogleTokenAsync(It.IsAny<VerifyTokenRequestDto>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ResponseResultDto<VerifyGoogleTokenDto>
            {
                Success = true,
                Data = new VerifyGoogleTokenDto
                {
                    GoogleUserExtraInfo = new GoogleUserExtraInfo
                    {
                        Id = Guid.NewGuid().ToString(),
                        AuthTime = new DateTime(),
                        Email = "",
                        GuardianType = "Email",
                        VerifiedEmail = true
                    },
                    Signature = "MockAppleSignature",
                    VerificationDoc = "MockAppleVerificationDoc"
                },
            });
        
        
        
        return mockVerifierServerClient.Object;
        
        
    }
    
    private IHttpClientFactory GetMockHttpClientFactory()
    {
        var clientHandlerStub = new DelegatingHandlerStub();
        var client = new HttpClient(clientHandlerStub);

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        var factory = mockFactory.Object;
        return factory;
    }
    
    private JwtSecurityTokenHandler GetJwtSecurityTokenHandlerMock()
    {
        var jwtSecurityTokenHandler = new Mock<JwtSecurityTokenHandler>();
        SecurityToken token = new JwtSecurityToken { Payload = { { "email_verified", "true" }, { "is_private_email", "false" } } };
        jwtSecurityTokenHandler.Setup(p => p.ValidateToken(It.IsAny<string>(),
                It.IsAny<TokenValidationParameters>(),
                out token))
            .Returns(SelectClaimsPrincipal());
        jwtSecurityTokenHandler.Setup(p => p.MaximumTokenSizeInBytes).Returns(1000000);
        jwtSecurityTokenHandler.Setup(p => p.CanReadToken(It.IsAny<string>())).Returns(true);
        return jwtSecurityTokenHandler.Object;
    }

    private IHttpClientService GetHttpClientService()
    {
        var mock = new Mock<IHttpClientService>();
        mock.Setup(p => p.GetAsync<AppleKeys>(It.IsAny<string>())).Returns(Task.FromResult(new AppleKeys
        {
            Keys = new List<AppleKey> { new AppleKey { Kid = "85205AB564C58D94B39785D0576515AB466E9581" } }
        }));
        return mock.Object;
    }

    private static ClaimsPrincipal SelectClaimsPrincipal()
    {
        IPrincipal currentPrincipal = Thread.CurrentPrincipal;
        return currentPrincipal is ClaimsPrincipal claimsPrincipal ? claimsPrincipal : (currentPrincipal == null ? (ClaimsPrincipal)null : new ClaimsPrincipal(currentPrincipal));
    }
    
    private ICacheProvider GetMockCacheProvider()
    {
        return new MockCacheProvider();
    }



    

    
    
        
}



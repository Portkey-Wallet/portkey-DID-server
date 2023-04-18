using System;
using System.Threading.Tasks;
using CAServer.Dtos;
using CAServer.Verifier;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace CAServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("CAVerifier")]
[Route("api/app/account")]
public class CAVerifierController : CAServerController
{
    private readonly IVerifierAppService _verifierAppService;
    private readonly IObjectMapper _objectMapper;

    public CAVerifierController(IVerifierAppService verifierAppService, IObjectMapper objectMapper)
    {
        _verifierAppService = verifierAppService;
        _objectMapper = objectMapper;
    }

    [HttpPost("sendVerificationRequest")]
    public async Task<VerifierServerResponse> SendVerificationRequest(VerifierServerInput verifierServerInput)
    {
        var sendVerificationRequestInput = _objectMapper.Map<VerifierServerInput, SendVerificationRequestInput>(verifierServerInput);
        return  await _verifierAppService.SendVerificationRequestAsync(sendVerificationRequestInput);
        
    }


    [HttpPost("verifyCode")]
    public async Task<VerificationCodeResponse> VerifyCode(VerificationSignatureRequestDto requestDto)
    {
        return await _verifierAppService.VerifyCodeAsync(requestDto);
    }
}
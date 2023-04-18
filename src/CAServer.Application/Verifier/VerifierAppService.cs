using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.AccountValidator;
using CAServer.Dtos;
using CAServer.Verifier.Dtos;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.ObjectMapping;

namespace CAServer.Verifier;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class VerifierAppService : CAServerAppService, IVerifierAppService
{
    private readonly IEnumerable<IAccountValidator> _accountValidator;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<VerifierAppService> _logger;
    private readonly IVerifierServerClient _verifierServerClient;

    public VerifierAppService(IEnumerable<IAccountValidator> accountValidator, IObjectMapper objectMapper,
        ILogger<VerifierAppService> logger,
        IVerifierServerClient verifierServerClient)
    {
        _accountValidator = accountValidator;
        _objectMapper = objectMapper;
        _logger = logger;
        _verifierServerClient = verifierServerClient;
    }

    public async Task<VerifierServerResponse> SendVerificationRequestAsync(SendVerificationRequestInput input)
    {
        //validate 
        var startTime = DateTime.UtcNow.ToUniversalTime();
        try
        {
            ValidateAccount(input);
            var verifierSessionId = Guid.NewGuid();
            input.VerifierSessionId = verifierSessionId;
            var dto = _objectMapper.Map<SendVerificationRequestInput, VerifierCodeRequestDto>(input);
            var result = await _verifierServerClient.SendVerificationRequestAsync(dto);
            if (result.Success)
            {
                return new VerifierServerResponse
                {
                    VerifierSessionId = verifierSessionId
                };
            }
            _logger.LogError("Send VerifierCode Failed : {message}", result.Message);
            throw new UserFriendlyException(result.Message);
        }
        catch (Exception e)
        {
            var endTime = DateTime.UtcNow.ToUniversalTime();
            var costTime = (startTime - endTime).TotalMilliseconds;
            _logger.LogDebug("TotalCount Time is {time}",(long)costTime);
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }

    }

    public async Task<VerificationCodeResponse> VerifyCodeAsync(VerificationSignatureRequestDto signatureRequestDto)
    {
        try
        {
            var request =
                _objectMapper.Map<VerificationSignatureRequestDto, VierifierCodeRequestInput>(signatureRequestDto);
            var response = await _verifierServerClient.VerifyCodeAsync(request);
            if (!response.Success)
            {
                throw new UserFriendlyException("Validate VerifierCode Failed :" + response.Message);
            }

            return new VerificationCodeResponse
            {
                VerificationDoc = response.Data.VerificationDoc,
                Signature = response.Data.Signature
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{Message}", e.Message);
            throw new UserFriendlyException(e.Message);
        }
    }

    private void ValidateAccount(SendVerificationRequestInput input)
    {
        var validator = _accountValidator.FirstOrDefault(v => v.Type == input.Type);
        if (validator == null)
        {
            throw new UserFriendlyException("InvalidInput type.");
        }

        if (!validator.Validate(input.GuardianAccount))
        {
            throw new UserFriendlyException("InvalidInput GuardianAccount");
        }
    }
}
using System;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Account;
using CAServer.CAAccount.Enums;
using CAServer.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace CAServer.CAAccount.Provider;

public class SignaturePreValidationProvider : CAServerAppService, IPreValidationStrategy
{
    private readonly ILogger<SignaturePreValidationProvider> _logger;
    private readonly IContractProvider _contractProvider;
    
    public PreValidationType Type => PreValidationType.Signature;
    
    public SignaturePreValidationProvider(
        ILogger<SignaturePreValidationProvider> logger,
        IContractProvider contractProvider)
    {
        _logger = logger;
        _contractProvider = contractProvider;
    }
    
    public bool ValidateParameters(GuardianInfo guardian)
    {
        return guardian is { VerificationInfo: not null }
               && !guardian.VerificationInfo.Id.IsNullOrEmpty()
               && !guardian.VerificationInfo.VerificationDoc.IsNullOrEmpty()
               && !guardian.VerificationInfo.Signature.IsNullOrEmpty();
    }

    public async Task<bool> PreValidateGuardian(string chainId, string caHash, string manager, GuardianInfo guardian)
    {
        var guardianInfo = ObjectMapper.Map<GuardianInfo, Portkey.Contracts.CA.GuardianInfo>(guardian);
        var result = new BoolValue()
        {
            Value = false
        };
        try
        {
            result = await _contractProvider.VerifySignature(chainId, guardianInfo, methodName:nameof(OperationTypeInContract.SocialRecovery).ToLower(), Hash.LoadFromHex(caHash), operationDetails:manager);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "_contractProvider.VerifySignature failed");
        }
        return result.Value;
    }
}
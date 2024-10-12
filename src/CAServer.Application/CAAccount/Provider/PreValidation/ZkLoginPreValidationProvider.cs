using System;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Account;
using CAServer.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;

namespace CAServer.CAAccount.Provider;

public class ZkLoginPreValidationProvider : CAServerAppService, IPreValidationStrategy
{
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly ILogger<ZkLoginPreValidationProvider> _logger;
    private readonly IContractProvider _contractProvider;
    
    public PreValidationType Type => PreValidationType.ZkLogin;
    
    public ZkLoginPreValidationProvider(IZkLoginProvider zkLoginProvider,
        ILogger<ZkLoginPreValidationProvider> logger,
        IContractProvider contractProvider)
    {
        _zkLoginProvider = zkLoginProvider;
        _logger = logger;
        _contractProvider = contractProvider;
    }
    
    public bool ValidateParameters(GuardianInfo guardian)
    {
        return _zkLoginProvider.CanExecuteZkByZkLoginInfoDto(guardian.Type, guardian.ZkLoginInfo);
    }

    public async Task<bool> PreValidateGuardian(string chainId, string caHash, string manager, GuardianInfo guardian)
    {
        var guardianInfo = ObjectMapper.Map<GuardianInfo, Portkey.Contracts.CA.GuardianInfo>(guardian);
        BoolValue result = new BoolValue()
        {
            Value = false
        };
        try
        {
            result = await _contractProvider.VerifyZkLogin(chainId, guardianInfo, Hash.LoadFromHex(caHash));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "_contractProvider.VerifyZkLogin failed caHash = {0}", caHash);
        }
        return result.Value;
    }
}
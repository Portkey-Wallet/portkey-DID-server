using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using CAServer.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.CAAccount.Provider;

public class ZkLoginPreValidationProvider : CAServerAppService, IPreValidationStrategy
{
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZkLoginProverOptions _zkLoginProverOptions;
    private readonly ILogger<ZkLoginPreValidationProvider> _logger;
    private readonly IContractProvider _contractProvider;
    
    public PreValidationType Type => PreValidationType.ZkLogin;
    
    public ZkLoginPreValidationProvider(IZkLoginProvider zkLoginProvider,
        IHttpClientFactory httpClientFactory,
        IOptions<ZkLoginProverOptions> zkLoginProverOptions,
        ILogger<ZkLoginPreValidationProvider> logger,
        IContractProvider contractProvider)
    {
        _zkLoginProvider = zkLoginProvider;
        _httpClientFactory = httpClientFactory;
        _zkLoginProverOptions = zkLoginProverOptions.Value;
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
        BoolValue result;
        try
        {
            result = await _contractProvider.VerifyZkLogin(chainId, guardianInfo, Hash.LoadFromHex(caHash));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "_contractProvider.VerifyZkLogin failed");
            throw new UserFriendlyException(e.Message);
        }
        return result.Value;
    }
    
    public async Task<bool> PreValidateGuardianRelyingOnProver(string chainId, string caHash, string manager, GuardianInfo guardian)
    {
        var client = _httpClientFactory.CreateClient();
        var parameters = JsonConvert.SerializeObject(new Dictionary<string, string>
        {
            { "identifierHash", guardian.ZkLoginInfo.PoseidonIdentifierHash },
            { "salt", guardian.ZkLoginInfo.Salt },
            { "nonce", guardian.ZkLoginInfo.Nonce },
            { "kid", guardian.ZkLoginInfo.Kid },
            { "proof", guardian.ZkLoginInfo.ZkProof }
        });
        
        var param = new StringContent(parameters, Encoding.UTF8, MediaTypeNames.Application.Json);
        
        string result = null;
        HttpResponseMessage response = null;
        try {
            response = await client.PostAsync(GetProverUrl(), param);
            result = await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e,"PreValidateGuardian zklogin post error url:{0} params:{1}", GetProverUrl(), parameters);
        }
        
        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.LogError("{Message}", "ZkLogin Prover return empty.");
            throw new UserFriendlyException("ZkLogin Prover return empty.");
        }
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("{Message}", $"ZkLogin Prover fail: {result}");
            throw new UserFriendlyException(result);
        }
        var proverResponse = JsonConvert.DeserializeObject<ProverResponse>(result);
        _logger.LogInformation("zklogin preValidationStrategy result:{5} type:{0} chainId:{1} caHash:{2} manager:{3} guardianInfo:{4}",
            proverResponse.Valid, PreValidationType.ZkLogin, chainId, caHash, manager, JsonConvert.SerializeObject(guardian));
        return proverResponse.Valid;
    }

    private string GetProverUrl()
    {
        return _zkLoginProverOptions.Domain + "/v1/verify";
    }
}
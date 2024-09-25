using System;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using CAServer.Account;
using CAServer.CAAccount.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;

namespace CAServer.CAAccount.Provider;

public class ZkLoginPreValidationProvider : IPreValidationStrategy
{
    private readonly IZkLoginProvider _zkLoginProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZkLoginProverOptions _zkLoginProverOptions;
    private readonly ILogger<ZkLoginPreValidationProvider> _logger;
    
    public ZkLoginPreValidationProvider(IZkLoginProvider zkLoginProvider,
        IHttpClientFactory httpClientFactory,
        IOptions<ZkLoginProverOptions> zkLoginProverOptions,
        ILogger<ZkLoginPreValidationProvider> logger)
    {
        _zkLoginProvider = zkLoginProvider;
        _httpClientFactory = httpClientFactory;
        _zkLoginProverOptions = zkLoginProverOptions.Value;
        _logger = logger;
    }
    
    public bool ValidateParameters(GuardianInfo guardian)
    {
        return _zkLoginProvider.CanExecuteZkByZkLoginInfoDto(guardian.Type, guardian.ZkLoginInfo);
    }

    public async Task<bool> PreValidateGuardian(string chainId, string caHash, string manager, GuardianInfo guardian)
    {
        var client = _httpClientFactory.CreateClient();
        var parameters = JsonConvert.SerializeObject(new
        {
            guardian.ZkLoginInfo.IdentifierHash, guardian.ZkLoginInfo.Salt,
            guardian.ZkLoginInfo.Nonce, guardian.ZkLoginInfo.Kid, guardian.ZkLoginInfo.ZkProof
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
        return proverResponse.Valid;
    }

    private string GetProverUrl()
    {
        return _zkLoginProverOptions.Domain + "/v1/verify";
    }
}
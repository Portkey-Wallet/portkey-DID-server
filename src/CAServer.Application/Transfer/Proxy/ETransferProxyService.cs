using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Transfer.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace CAServer.Transfer.Proxy;

public class ETransferProxyService : IETransferProxyService, ISingletonDependency
{
    private readonly IETransferClientProvider _clientProvider;
    private readonly ILogger<ETransferProxyService> _logger;
    private readonly ETransferOptions _options;
    private readonly IObjectMapper _objectMapper;

    public ETransferProxyService(
        ILogger<ETransferProxyService> logger, IOptionsSnapshot<ETransferOptions> options,
        IETransferClientProvider clientProvider, IObjectMapper objectMapper)
    {
        _logger = logger;
        _clientProvider = clientProvider;
        _objectMapper = objectMapper;
        _options = options.Value;
    }

    public async Task<AuthTokenDto> GetConnectTokenAsync(AuthTokenRequestDto request)
    {
        var url =
            $"{_options.AuthBaseUrl.TrimEnd('/')}/{_options.AuthPrefix}{ETransferConstant.GetConnectToken.TrimStart('/')}";
        var requestParam = _objectMapper.Map<AuthTokenRequestDto, ETransferAuthTokenRequestDto>(request);
        return await _clientProvider.PostFormAsync<AuthTokenDto>(url, requestParam);
    }

    public async Task<ResponseWrapDto<WithdrawTokenListDto>> GetTokenListAsync(WithdrawTokenListRequestDto request)
    {
        return await _clientProvider.GetAsync<ResponseWrapDto<WithdrawTokenListDto>>(ETransferConstant.GetTokenList,
            request);
    }

    public async Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(
        GetTokenOptionListRequestDto request)
    {
        return await _clientProvider.GetAsync<ResponseWrapDto<GetTokenOptionListDto>>(
            ETransferConstant.GetTokenOptionList, request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        if (request.Symbol.IsNullOrEmpty())
        {
            var tokenListWrap = await GetTokenOptionListAsync(new GetTokenOptionListRequestDto()
            {
                Type = "Deposit"
            });

            if (tokenListWrap.Code != "20000")
            {
                throw new UserFriendlyException(tokenListWrap.Message);
            }

            var dto = tokenListWrap.Data;
            var tokens = dto.TokenList;

            var networkResp = new List<NetworkDto>();

            foreach (var tokenDto in tokens)
            {
                // get network
                var networkInfosWrap = await GetNetworkListWithSymbolAsync(new GetNetworkListRequestDto()
                {
                    ChainId = request.ChainId,
                    Type = "Deposit",
                    Symbol = tokenDto.Symbol
                });

                if (networkInfosWrap.Code != "20000")
                {
                    if (networkInfosWrap.Message.Contains("Invalid symbol"))
                    {
                        continue;
                    }

                    throw new UserFriendlyException(networkInfosWrap.Message);
                }

                var getNetworkListDto = networkInfosWrap.Data.NetworkList;
                var networkNames = networkResp.Select(t => t.Network).ToList();
                getNetworkListDto = getNetworkListDto.Where(t => !networkNames.Contains(t.Network)).ToList();

                networkResp.AddRange(getNetworkListDto);
            }

            return new ResponseWrapDto<GetNetworkListDto>()
            {
                Code = "20000",
                Data = new GetNetworkListDto()
                {
                    ChainId = request.ChainId,
                    NetworkList = networkResp
                }
            };
        }

        return await _clientProvider.GetAsync<ResponseWrapDto<GetNetworkListDto>>(ETransferConstant.GetNetworkList,
            request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListWithSymbolAsync(
        GetNetworkListRequestDto request)
    {
        var url = ETransferConstant.GetNetworkList +
                  $"?type={request.Type}&chainId={request.ChainId}&symbol={request.Symbol}";

        return await _clientProvider.GetAsync<ResponseWrapDto<GetNetworkListDto>>(url,
            request);
    }

    public async Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(
        GetCalculateDepositRateRequestDto request)
    {
        return await _clientProvider.GetAsync<ResponseWrapDto<CalculateDepositRateDto>>(
            ETransferConstant.CalculateDepositRate, request);
    }

    public async Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        return await _clientProvider.GetAsync<ResponseWrapDto<GetDepositInfoDto>>(ETransferConstant.GetDepositInfo,
            request);
    }

    public async Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        var response = new GetNetworkTokensDto();
        var tokenList = new List<NetworkTokenInfo>();


        var url = ETransferConstant.GetTokenOptionList + "?type=Deposit";

        var tokenListWrap = await _clientProvider.GetAsync<ResponseWrapDto<GetTokenOptionListDto>>(url, request);

        if (tokenListWrap.Code != "20000")
        {
            throw new UserFriendlyException(tokenListWrap.Message);
        }

        var dto = tokenListWrap.Data;
        var tokens = dto.TokenList;

        if (request.Type == "to")
        {
            foreach (var tokenDto in tokens)
            {
                var tokenInnerInfo = tokenDto.ToTokenList
                    .Where(t => request.Network.IsNullOrEmpty() || t.ChainIdList.Contains(request.Network)).ToList();

                foreach (var item in tokenInnerInfo)
                {
                    tokenList.Add(new NetworkTokenInfo()
                    {
                        Name = item.Name,
                        Icon = item.Icon,
                        Symbol = item.Symbol
                    });
                }
            }

            response.TokenList = tokenList;
            return new ResponseWrapDto<GetNetworkTokensDto>()
            {
                Code = "20000",
                Data = response
            };
        }

        var getNetworkListDto = new List<NetworkDto>();

        foreach (var tokenDto in tokens)
        {
            // get network
            var networkInfosWrap = await GetNetworkListWithSymbolAsync(new GetNetworkListRequestDto()
            {
                ChainId = request.ChainId,
                Type = "Deposit",
                Symbol = tokenDto.Symbol
            });

            if (networkInfosWrap.Code != "20000")
            {
                throw new UserFriendlyException(networkInfosWrap.Message);
            }

            getNetworkListDto = networkInfosWrap.Data.NetworkList;
            if (getNetworkListDto.FirstOrDefault(t =>
                    request.Network.IsNullOrEmpty() || t.Network == request.Network) != null)
            {
                tokenList.Add(new NetworkTokenInfo()
                {
                    Name = tokenDto.Name,
                    ContractAddress = tokenDto.ContractAddress,
                    Symbol = tokenDto.Symbol,
                    Icon = tokenDto.Icon,
                    NetworkList = getNetworkListDto
                });
            }
        }

        response.TokenList = tokenList;
        return new ResponseWrapDto<GetNetworkTokensDto>()
        {
            Code = "20000",
            Data = response
        };
    }
}
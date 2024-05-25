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
using Volo.Abp.Application.Dtos;
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
        return await _clientProvider.GetAsync<WithdrawTokenListDto>(ETransferConstant.GetTokenList,
            request);
    }

    public async Task<ResponseWrapDto<GetTokenOptionListDto>> GetTokenOptionListAsync(
        GetTokenOptionListRequestDto request)
    {
        return await _clientProvider.GetAsync<GetTokenOptionListDto>(
            ETransferConstant.GetTokenOptionList, request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        if (request.Symbol.IsNullOrEmpty())
        {
            var tokenListWrap = await GetTokenOptionListAsync(new GetTokenOptionListRequestDto()
            {
                Type = ETransferConstant.DepositName
            });

            if (tokenListWrap.Code != ETransferConstant.SuccessCode)
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
                    Type = ETransferConstant.DepositName,
                    Symbol = tokenDto.Symbol
                });

                if (networkInfosWrap.Code != ETransferConstant.SuccessCode)
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
                Code = ETransferConstant.SuccessCode,
                Data = new GetNetworkListDto()
                {
                    ChainId = request.ChainId,
                    NetworkList = networkResp
                }
            };
        }

        return await _clientProvider.GetAsync<GetNetworkListDto>(ETransferConstant.GetNetworkList,
            request);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListWithSymbolAsync(
        GetNetworkListRequestDto request)
    {
        var url = ETransferConstant.GetNetworkList +
                  $"?type={request.Type}&chainId={request.ChainId}&symbol={request.Symbol}";

        return await _clientProvider.GetAsync<GetNetworkListDto>(url,
            request);
    }

    public async Task<ResponseWrapDto<CalculateDepositRateDto>> CalculateDepositRateAsync(
        GetCalculateDepositRateRequestDto request)
    {
        return await _clientProvider.GetAsync<CalculateDepositRateDto>(
            ETransferConstant.CalculateDepositRate, request);
    }

    public async Task<ResponseWrapDto<GetDepositInfoDto>> GetDepositInfoAsync(GetDepositRequestDto request)
    {
        return await _clientProvider.GetAsync<GetDepositInfoDto>(ETransferConstant.GetDepositInfo,
            request);
    }

    public async Task<ResponseWrapDto<GetNetworkTokensDto>> GetNetworkTokensAsync(GetNetworkTokensRequestDto request)
    {
        var response = new ResponseWrapDto<GetNetworkTokensDto>
        {
            Code = ETransferConstant.SuccessCode,
            Data = new GetNetworkTokensDto()
        };
        var tokenList = new List<NetworkTokenInfo>();


        var url = ETransferConstant.GetTokenOptionList + "?type=Deposit";

        var tokenListWrap = await _clientProvider.GetAsync<GetTokenOptionListDto>(url, request);

        if (tokenListWrap.Code != ETransferConstant.SuccessCode)
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
                    .Where(t => request.ChainId.IsNullOrEmpty() || t.ChainIdList.Contains(request.ChainId)).ToList();

                foreach (var item in tokenInnerInfo)
                {
                    var networkList = new List<NetworkDto>();
                    item.ChainIdList?.ForEach(chainId =>
                    {
                        networkList.Add(new NetworkDto()
                        {
                            Name = chainId,
                            Network = chainId
                        });
                    });
                    tokenList.Add(new NetworkTokenInfo()
                    {
                        Name = item.Name,
                        Icon = item.Icon,
                        Symbol = item.Symbol,
                        ContractAddress = string.Empty,
                        NetworkList = networkList
                    });
                }
            }

            response.Data.TokenList = tokenList.DistinctBy(t => t.Symbol).ToList();
            return response;
        }

        var getNetworkListDto = new List<NetworkDto>();

        foreach (var tokenDto in tokens)
        {
            // get network
            var networkInfosWrap = await GetNetworkListWithSymbolAsync(new GetNetworkListRequestDto()
            {
                ChainId = "tDVW",
                Type = ETransferConstant.DepositName,
                Symbol = tokenDto.Symbol
            });

            if (networkInfosWrap.Code != ETransferConstant.SuccessCode)
            {
                throw new UserFriendlyException(networkInfosWrap.Message);
            }

            getNetworkListDto = networkInfosWrap.Data.NetworkList;

            var networkTokenDto =
                getNetworkListDto.FirstOrDefault(t => !request.Network.IsNullOrEmpty() && t.Network == request.Network);
            if (networkTokenDto != null || request.Network.IsNullOrEmpty())
            {
                tokenList.Add(new NetworkTokenInfo()
                {
                    Name = tokenDto.Name,
                    ContractAddress = request.Network.IsNullOrEmpty() ? string.Empty : networkTokenDto?.ContractAddress,
                    Symbol = tokenDto.Symbol,
                    Icon = tokenDto.Icon,
                    NetworkList = request.Network.IsNullOrEmpty() ? getNetworkListDto : new List<NetworkDto>()
                });
            }
        }

        response.Data.TokenList = tokenList;
        return response;
    }

    public async Task<ResponseWrapDto<PagedResultDto<OrderIndexDto>>> GetRecordListAsync(
        GetOrderRecordRequestDto request)
    {
        var url = GetUrl(ETransferConstant.GetOrderRecordList, request);
        return await _clientProvider.GetAsync<PagedResultDto<OrderIndexDto>>(url);
    }

    private string GetUrl(string uri, object reqParam)
    {
        var url = uri.StartsWith(CommonConstant.ProtocolName)
            ? uri
            : $"{_options.BaseUrl.TrimEnd('/')}/{_options.Prefix}/{uri.TrimStart('/')}";

        if (reqParam == null)
        {
            return url;
        }

        url += "?";
        var props = reqParam.GetType().GetProperties();
        foreach (var prop in props)
        {
            var val = prop.GetValue(reqParam, null);
            if (val == null) continue;

            var key = prop.Name;

            url += $"{key}={val}&";
        }

        url = url.TrimEnd('?', '&');
        return url;
    }
}
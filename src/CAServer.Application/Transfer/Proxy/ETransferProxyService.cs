using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.CrossChain;
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
    private readonly ChainOptions _chainOptions;

    public ETransferProxyService(
        ILogger<ETransferProxyService> logger, IOptionsSnapshot<ETransferOptions> options,
        IETransferClientProvider clientProvider, IObjectMapper objectMapper,
        IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _logger = logger;
        _clientProvider = clientProvider;
        _objectMapper = objectMapper;
        _options = options.Value;
        _chainOptions = chainOptions.Value;
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
        var url = GetUrl(ETransferConstant.GetTokenOptionList, request);
        return await _clientProvider.GetAsync<GetTokenOptionListDto>(url);
    }

    public async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListAsync(GetNetworkListRequestDto request)
    {
        var isMainChain = IsMainChain(request.ChainId);
        if (!request.Symbol.IsNullOrEmpty())
        {
            var url = GetUrl(ETransferConstant.GetNetworkList, request);
            var wrapDto = await _clientProvider.GetAsync<GetNetworkListDto>(url,
                request);
            var list = ReRangeList(wrapDto.Data.NetworkList, isMainChain, request.Symbol);
            var resultList = list.Where(t => t.Status == "Health").ToList();
            wrapDto.Data.NetworkList = resultList;
            return wrapDto;
        }
        
        var networkList = await GetAllNetworkAsync(request);
        var newNetworkList = networkList.Where(t => t.Status == "Health").ToList();
        var reRangeList = ReRangeList(newNetworkList, isMainChain, request.Symbol);
        return new ResponseWrapDto<GetNetworkListDto>
        {
            
            Code = ETransferConstant.SuccessCode,
            Data = new GetNetworkListDto
            {
                ChainId = request.ChainId,
                NetworkList = reRangeList
            }
        };
    }

    private async Task<List<NetworkDto>> GetAllNetworkAsync(GetNetworkListRequestDto request)
    {
        var tokenListWrap = await GetTokenOptionListAsync(new GetTokenOptionListRequestDto()
        {
            Type = ETransferConstant.DepositName
        });

        if (tokenListWrap.Code != ETransferConstant.SuccessCode)
        {
            throw new UserFriendlyException(tokenListWrap.Message);
        }

        var networkList = new List<NetworkDto>();
        foreach (var tokenDto in tokenListWrap.Data.TokenList)
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

            var networkNames = networkList.Select(t => t.Network).ToList();
            var networkListDto = networkInfosWrap.Data.NetworkList.Where(t => !networkNames.Contains(t.Network))
                .ToList();

            networkList.AddRange(networkListDto);
        }

        return networkList;
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
        if (request.Type == ETransferConstant.ToType)
        {
            tokenList = await GetToNetworkTokenInfosAsync(request, dto.TokenList);
            response.Data.TokenList = tokenList.DistinctBy(t => t.Symbol).ToList();
            return response;
        }

        tokenList = await GetFromNetworkTokenInfosAsync(request, dto.TokenList);
        response.Data.TokenList = tokenList;
        return response;
    }

    private async Task<List<NetworkTokenInfo>> GetFromNetworkTokenInfosAsync(GetNetworkTokensRequestDto request,
        List<TokenOptionConfigDto> tokens)
    {
        if (!request.ChainId.IsNullOrEmpty())
        {
            return await GetTokenInfosAsync(tokens, request.Network, request.ChainId);
        }

        var tokenList = new List<NetworkTokenInfo>();
        foreach (var chainId in _chainOptions.ChainInfos.Keys)
        {
            var tokensDto = await GetTokenInfosAsync(tokens, request.Network, chainId);

            var symbols = tokenList.Select(t => t.Symbol).ToList();
            tokensDto = tokensDto.Where(t => !symbols.Contains(t.Symbol)).ToList();
            tokenList.AddRange(tokensDto);
        }

        return tokenList;
    }

    private async Task<List<NetworkTokenInfo>> GetTokenInfosAsync(List<TokenOptionConfigDto> tokens,
        string network, string chainId)
    {
        var tokenList = new List<NetworkTokenInfo>();
        foreach (var tokenDto in tokens)
        {
            // get network
            var networkInfosWrap = await GetNetworkListWithSymbolAsync(new GetNetworkListRequestDto()
            {
                ChainId = chainId,
                Type = ETransferConstant.DepositName,
                Symbol = tokenDto.Symbol
            });

            if (networkInfosWrap.Code != ETransferConstant.SuccessCode)
            {
                throw new UserFriendlyException(networkInfosWrap.Message);
            }

            var networkList = networkInfosWrap.Data.NetworkList;
            var networkTokenDto =
                networkList.FirstOrDefault(t => !network.IsNullOrEmpty() && t.Network == network);
            if (networkTokenDto != null || network.IsNullOrEmpty())
            {
                if (tokenList.FirstOrDefault(t => t.Symbol == tokenDto.Symbol) != null)
                {
                    continue;
                }

                tokenList.Add(new NetworkTokenInfo()
                {
                    Name = tokenDto.Name,
                    ContractAddress = network.IsNullOrEmpty()
                        ? string.Empty
                        : networkTokenDto?.ContractAddress,
                    Symbol = tokenDto.Symbol,
                    Icon = tokenDto.Icon,
                    NetworkList = network.IsNullOrEmpty() ? networkList : new List<NetworkDto>()
                });
            }
        }

        return tokenList;
    }

    private async Task<List<NetworkTokenInfo>> GetToNetworkTokenInfosAsync(GetNetworkTokensRequestDto request,
        List<TokenOptionConfigDto> tokens)
    {
        var tokenList = new List<NetworkTokenInfo>();
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

        return tokenList;
    }

    public async Task<ResponseWrapDto<PagedResultDto<OrderIndexDto>>> GetRecordListAsync(
        GetOrderRecordRequestDto request)
    {
        var url = GetUrl(ETransferConstant.GetOrderRecordList, request);
        return await _clientProvider.GetAsync<PagedResultDto<OrderIndexDto>>(url);
    }

    private async Task<ResponseWrapDto<GetNetworkListDto>> GetNetworkListWithSymbolAsync(
        GetNetworkListRequestDto request)
    {
        var url = ETransferConstant.GetNetworkList +
                  $"?type={request.Type}&chainId={request.ChainId}&symbol={request.Symbol}";

        return await _clientProvider.GetAsync<GetNetworkListDto>(url,
            request);
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

    private bool IsMainChain(string chainId)
    {
        var currentChainId = ChainHelper.ConvertBase58ToChainId(chainId);
        var chainInfoList = _chainOptions.ChainInfos.Where(t => t.Value.IsMainChain).ToList();
        var chainIdStr = chainInfoList.FirstOrDefault().Key;
        return currentChainId == ChainHelper.ConvertBase58ToChainId(chainIdStr);
    }

    private List<NetworkDto> ReRangeList(List<NetworkDto> list,bool isMainChain,string symbol)
    {
        if (isMainChain && symbol == ETransferConstant.DefaultToken)
        {
            return list;
        }

        var networkDto = list.Where(t=>t.Network == ETransferConstant.TronName).ToList().FirstOrDefault();
        var index = list.IndexOf(networkDto);
        if (index == -1)
        {
            return list;
        }

        list.Remove(networkDto); 
        list.Insert(0, networkDto);
        return list;

    }


}
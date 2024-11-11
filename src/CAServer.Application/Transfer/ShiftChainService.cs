using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Options;
using CAServer.Tokens;
using CAServer.Transfer.Dtos;
using CAServer.Transfer.Proxy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace CAServer.Transfer;

[RemoteService(false), DisableAuditing]
public class ShiftChainService : CAServerAppService, IShiftChainService
{
    private readonly IETransferProxyService _eTransferProxyService;
    private readonly ChainOptions _chainOptions;
    private readonly ITokenAppService _tokenAppService;
    private readonly IHttpClientService _httpClientService;
    private readonly ETransferOptions _eTransferOptions;
    private readonly INetworkCacheService _networkCacheService;
    private readonly TransferAppService _transferAppService;
    private readonly ILogger<ShiftChainService> _logger;

    public ShiftChainService(IETransferProxyService eTransferProxyService,
        IOptionsSnapshot<ChainOptions> chainOptions, ITokenAppService tokenAppService, IHttpClientService httpClientService,
        IOptionsSnapshot<ETransferOptions> eTransferOptions, INetworkCacheService networkCacheService, TransferAppService transferAppService,
        ILogger<ShiftChainService> logger)
    {
        _eTransferProxyService = eTransferProxyService;
        _chainOptions = chainOptions.Value;
        _tokenAppService = tokenAppService;
        _httpClientService = httpClientService;
        _eTransferOptions = eTransferOptions.Value;
        _networkCacheService = networkCacheService;
        _transferAppService = transferAppService;
        _logger = logger;
    }

    public async Task Init()
    {
        Dictionary<string, ReceiveNetworkDto> receiveNetworkMap = new Dictionary<string, ReceiveNetworkDto>();
        Dictionary<string, NetworkInfoDto> networkMap = new Dictionary<string, NetworkInfoDto>();
        Dictionary<string, SendNetworkDto> sendEBridgeMap = new Dictionary<string, SendNetworkDto>();

        await setReceiveByETransfer(receiveNetworkMap, networkMap);

        var limiter = await setReceiveByEBridge(receiveNetworkMap, networkMap);
        setSendByEBridge(sendEBridgeMap, networkMap, limiter);

        foreach (var chainName in _chainOptions.ChainInfos.Keys)
        {
            networkMap[chainName] = ShiftChainHelper.GetAELFInfo(chainName);
        }

        _networkCacheService.SetCache(receiveNetworkMap, networkMap, sendEBridgeMap);
    }


    private readonly SemaphoreSlim _semaphoreReceive = new SemaphoreSlim(1, 1);

    public async Task<ResponseWrapDto<ReceiveNetworkDto>> GetReceiveNetworkList(GetReceiveNetworkListRequestDto request)
    {
        var result = _networkCacheService.GetReceiveNetworkList(request);
        if (null == result)
        {
            await _semaphoreReceive.WaitAsync();
            try
            {
                result = _networkCacheService.GetReceiveNetworkList(request);
                if (null == result)
                {
                    await Init();
                    result = _networkCacheService.GetReceiveNetworkList(request);
                }
            }
            finally
            {
                _semaphoreReceive.Release();
            }
        }

        return new ResponseWrapDto<ReceiveNetworkDto>
        {
            Code = ETransferConstant.SuccessCode,
            Data = result
        };
    }

    public async Task<ResponseWrapDto<SendNetworkDto>> GetSendNetworkList(GetSendNetworkListRequestDto request)
    {
        SendNetworkDto result = new SendNetworkDto { NetworkList = new List<NetworkInfoDto>() };
        if (ShiftChainHelper.GetAddressFormat(request.ChainId, request.ToAddress) == AddressFormat.Main ||
            ShiftChainHelper.GetAddressFormat(request.ChainId, request.ToAddress) == AddressFormat.Dapp)
        {
            result.NetworkList.Add(_networkCacheService.GetNetwork(request.ChainId));
        }

        await setSendByETransfer(result, request);

        await setSendByEBridge(result, request);

        return new ResponseWrapDto<SendNetworkDto>
        {
            Code = ETransferConstant.SuccessCode,
            Data = result
        };
    }

    private async Task setSendByEBridge(SendNetworkDto result, GetSendNetworkListRequestDto request)
    {
        // set ebridge
        var ebridge = _networkCacheService.GetSendNetworkList(request);
        if (ebridge?.Count > 0)
        {
            foreach (var network in ebridge)
            {
                var service = new ServiceDto
                {
                    ServiceName = ShiftChainHelper.EBridgeTool,
                    MultiConfirmTime = ShiftChainHelper.GetTime(request.ChainId, network.Network),
                };
                var orgNet = result.NetworkList.FirstOrDefault(p => p.Network == network.Network);
                if (null == orgNet)
                {
                    orgNet = new NetworkInfoDto
                    {
                        Name = network.Name,
                        Network = network.Network,
                        ImageUrl = ShiftChainHelper.GetChainImage(network.Network),
                        ServiceList = new List<ServiceDto> { service }
                    };
                    result.NetworkList.Add(orgNet);
                }
                else
                {
                    orgNet.ServiceList.Add(service);
                }
            }
        }
    }

    private async Task setSendByETransfer(SendNetworkDto result, GetSendNetworkListRequestDto request)
    {
        // set etransfer
        string formatAddress = ShiftChainHelper.ExtractAddress(request.ToAddress);
        ResponseWrapDto<GetNetworkListDto> etransfer = null;
        try
        {
            etransfer = await _eTransferProxyService.GetNetworkListAsync(new GetNetworkListRequestDto
            {
                Type = "Withdraw", Symbol = request.Symbol, ChainId = request.ChainId, Address = formatAddress
            });
        }
        catch (Exception e)
        {
            return;
        }

        if (etransfer?.Data?.NetworkList?.Count != 0)
        {
            var price = await _tokenAppService.GetTokenPriceListAsync(new List<string> { request.Symbol });
            var maxAmount = ShiftChainHelper.GetMaxAmount(price.Items[0].PriceInUsd);
            foreach (var networkDto in etransfer.Data.NetworkList)
            {
                result.NetworkList.Add(new NetworkInfoDto
                {
                    Name = networkDto.Name,
                    Network = networkDto.Network,
                    ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                    ServiceList = new List<ServiceDto>
                    {
                        new ServiceDto
                        {
                            ServiceName = ShiftChainHelper.ETransferTool,
                            MultiConfirmTime = networkDto.MultiConfirmTime,
                            MaxAmount = maxAmount
                        }
                    }
                });
            }
        }
    }


    private async Task setReceiveByETransfer(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap, Dictionary<string, NetworkInfoDto> networkMap)
    {
        string type = "Deposit";
        var optionList = await _transferAppService.GetTokenOptionListAsync(new GetTokenOptionListRequestDto { Type = type });
        foreach (var token in optionList.Data.TokenList)
        {
            var toToken = token.ToTokenList.FirstOrDefault(p => p.Symbol.Equals(token.Symbol));
            if (toToken == null)
            {
                continue;
            }

            string symbol = token.Symbol;
            ReceiveNetworkDto receiveNetwork = initAELFChain(symbol);
            receiveNetworkMap[symbol] = receiveNetwork;
            var price = await _tokenAppService.GetTokenPriceListAsync(new List<string> { symbol });
            _logger.LogInformation("setReceiveByETransfer symbol = {0} price = {1}", symbol, JsonConvert.SerializeObject(price));
            var maxAmount = ShiftChainHelper.GetMaxAmount(price.Items[0].PriceInUsd);
            foreach (var chainId in toToken.ChainIdList)
            {
                var networkList = await _eTransferProxyService.GetNetworkListAsync(new GetNetworkListRequestDto
                {
                    Type = type, Symbol = token.Symbol, ChainId = chainId,
                });
                foreach (var networkDto in networkList.Data.NetworkList)
                {
                    receiveNetwork.DestinationMap[chainId].Add(new NetworkInfoDto
                    {
                        Network = networkDto.Network,
                        Name = networkDto.Name,
                        ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                        ServiceList = new List<ServiceDto>
                        {
                            new ServiceDto
                            {
                                ServiceName = ShiftChainHelper.ETransferTool,
                                MultiConfirmTime = networkDto.MultiConfirmTime,
                                MaxAmount = maxAmount
                            }
                        }
                    });
                    networkMap[networkDto.Network] = new NetworkInfoDto
                    {
                        Network = networkDto.Network,
                        Name = networkDto.Name,
                        ImageUrl = ShiftChainHelper.GetChainImage(networkDto.Network),
                    };
                }
            }
        }
    }

    private async Task<EBridgeLimiterDto> setReceiveByEBridge(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap,
        Dictionary<string, NetworkInfoDto> networkMap)
    {
        var limiters = await _httpClientService.GetAsync<EBridgeLimiterDto>(_eTransferOptions.EBridgeLimiterUrl);
        foreach (var limiter in limiters.Items)
        {
            foreach (var tokenInfo in limiter.ReceiptRateLimitsInfo)
            {
                if (tokenInfo.Token.Equals("AGENT"))
                {
                    continue;
                }

                if (!receiveNetworkMap.TryGetValue(tokenInfo.Token, out var network))
                {
                    network = initAELFChain(tokenInfo.Token);
                    receiveNetworkMap[tokenInfo.Token] = network;
                }

                if (!network.DestinationMap.ContainsKey(limiter.ToChain))
                {
                    continue;
                }

                NetworkInfoDto networkInfo = ShiftChainHelper.GetNetworkInfoByEBridge(networkMap, limiter.FromChain);
                var destinationNetworks = network.DestinationMap[limiter.ToChain];
                var destinationNetwork = destinationNetworks.FirstOrDefault(p => p.Network.Equals(networkInfo.Network));

                var serviceDto = new ServiceDto
                {
                    ServiceName = ShiftChainHelper.EBridgeTool,
                    MultiConfirmTime = ShiftChainHelper.GetTime(limiter.FromChain, limiter.ToChain)
                };
                if (destinationNetwork == null)
                {
                    destinationNetwork = new NetworkInfoDto
                    {
                        Network = networkInfo.Network,
                        Name = networkInfo.Name,
                        ImageUrl = networkInfo.ImageUrl,
                        ServiceList = new List<ServiceDto> { serviceDto }
                    };
                    destinationNetworks.Add(destinationNetwork);
                }
                else
                {
                    destinationNetwork.ServiceList.Add(serviceDto);
                }
            }
        }

        return limiters;
    }

    private void setSendByEBridge(Dictionary<string, SendNetworkDto> sendEBridgeMap, Dictionary<string, NetworkInfoDto> networkMap,
        EBridgeLimiterDto limiters)
    {
        foreach (var limiter in limiters.Items)
        {
            foreach (var tokenInfo in limiter.ReceiptRateLimitsInfo)
            {
                if (tokenInfo.Token.Equals("AGENT"))
                {
                    continue;
                }

                if (!CommonConstant.ChainIds.Contains(limiter.ToChain))
                {
                    continue;
                }

                string key = tokenInfo.Token + ";" + ShiftChainHelper.FormatEBridgeChain(limiter.ToChain);
                if (!sendEBridgeMap.TryGetValue(key, out var sendInfo))
                {
                    sendInfo = new SendNetworkDto { NetworkList = new List<NetworkInfoDto>() };
                    sendEBridgeMap[key] = sendInfo;
                }

                if (!sendInfo.NetworkList.Any(p => p.Network == limiter.FromChain))
                {
                    sendInfo.NetworkList.Add(ShiftChainHelper.GetNetworkInfoByEBridge(networkMap, limiter.FromChain));
                }
            }
        }
    }

    private ReceiveNetworkDto initAELFChain(string symbol)
    {
        ReceiveNetworkDto receiveNetwork = new ReceiveNetworkDto { DestinationMap = new Dictionary<string, List<NetworkInfoDto>>() };
        var chainIds = _chainOptions.ChainInfos.Keys;
        foreach (var chainId in chainIds)
        {
            receiveNetwork.DestinationMap[chainId] = new List<NetworkInfoDto>();
            foreach (var chainInfosKey in chainIds)
            {
                receiveNetwork.DestinationMap[chainId].Add(ShiftChainHelper.GetAELFInfo(chainInfosKey));
            }
        }

        return receiveNetwork;
    }
}
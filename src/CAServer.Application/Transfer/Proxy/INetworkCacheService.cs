using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.Commons;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Transfer.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace CAServer.Transfer.Proxy;

public interface INetworkCacheService
{
    void SetCache(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap, Dictionary<string, NetworkInfoDto> networkMap,
        Dictionary<string, SendNetworkDto> sendEBridgeMap);

    NetworkInfoDto GetNetwork(string network);
    ReceiveNetworkDto GetReceiveNetworkList(GetReceiveNetworkListRequestDto request);
    List<NetworkInfoDto> GetSendNetworkList(GetSendNetworkListRequestDto request);
    Dictionary<string, NetworkInfoDto> GetNetworkMap();
    Dictionary<string, ReceiveNetworkDto> GetReceiveNetworkMap();
}

public class NetworkCacheService : INetworkCacheService, ISingletonDependency
{
    private readonly ChainOptions _chainOptions;
    private Dictionary<string, ReceiveNetworkDto> _receiveNetworkMap;
    private Dictionary<string, NetworkInfoDto> _networkMap;
    private Dictionary<string, SendNetworkDto> _sendEBridgeMap;
    private long _lastCacheTime = 0L;
    private long _maxCacheTime = 60 * 60 * 1000L;

    public NetworkCacheService(IOptionsSnapshot<ChainOptions> chainOptions)
    {
        _chainOptions = chainOptions.Value;
    }

    public void SetCache(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap, Dictionary<string, NetworkInfoDto> networkMap,
        Dictionary<string, SendNetworkDto> sendEBridgeMap)
    {
        _receiveNetworkMap = receiveNetworkMap;
        // sore
        foreach (var receiveNetworkEntry in _receiveNetworkMap)
        {
            var destinationMap = receiveNetworkEntry.Value.DestinationMap;
            var keys = destinationMap.Keys.ToList();

            foreach (var key in keys)
            {
                destinationMap[key] = destinationMap[key]
                    .OrderBy(n => GetSortOrder(n.Network))
                    .ToList();
            }
        }
        _networkMap = networkMap;
        _sendEBridgeMap = sendEBridgeMap;
        _lastCacheTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    private int GetSortOrder(string network)
    {
        return network switch
        {
            CommonConstant.MainChainId => 1,
            CommonConstant.TDVVChainId => 0,
            CommonConstant.TDVWChainId => 0,
            _ => 2,
        };
    }
    public NetworkInfoDto GetNetwork(string network)
    {
        return _networkMap.TryGetValue(network, out var result) ? result : null;
    }

    public ReceiveNetworkDto GetReceiveNetworkList(GetReceiveNetworkListRequestDto request)
    {
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _lastCacheTime < _maxCacheTime)
        {
            if (!_receiveNetworkMap.TryGetValue(request.Symbol, out ReceiveNetworkDto result))
            {
                result = new ReceiveNetworkDto();

                var chainIds = _chainOptions.ChainInfos.Keys;
                foreach (var chainId in chainIds)
                {
                    result.DestinationMap[chainId] = new List<NetworkInfoDto>();
                    foreach (var chainInfosKey in chainIds)
                    {
                        result.DestinationMap[chainId].Add(ShiftChainHelper.GetAELFInfo(chainInfosKey));
                    }
                }
            }

            return result;
        }

        return null;
    }

    public List<NetworkInfoDto> GetSendNetworkList(GetSendNetworkListRequestDto request)
    {
        string key = request.Symbol + ";" + request.ChainId;
        if (_sendEBridgeMap.TryGetValue(key, out SendNetworkDto result))
        {
            return result.NetworkList.Where(p => ShiftChainHelper.MatchForAddress(p.Network, request.ChainId, request.ToAddress)).ToList();
        }

        return null;
    }

    public Dictionary<string, NetworkInfoDto> GetNetworkMap() => _networkMap;
    public Dictionary<string, ReceiveNetworkDto> GetReceiveNetworkMap() => _receiveNetworkMap;
}
using System;
using System.Collections.Generic;
using System.Linq;
using CAServer.Commons;
using CAServer.Transfer.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.Transfer.Proxy;

public interface INetworkCacheService
{
    void SetCache(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap, Dictionary<string, NetworkInfoDto> networkMap,
        Dictionary<string, SendNetworkDto> sendEBridgeMap);

    NetworkInfoDto GetNetwork(string network);
    ReceiveNetworkDto GetReceiveNetworkList(GetReceiveNetworkListRequestDto request);
    List<NetworkInfoDto> GetSendNetworkList(GetSendNetworkListRequestDto request);
}

public class NetworkCacheService : INetworkCacheService, ISingletonDependency
{
    private Dictionary<string, ReceiveNetworkDto> _receiveNetworkMap;
    private Dictionary<string, NetworkInfoDto> _networkMap;
    private Dictionary<string, SendNetworkDto> _sendEBridgeMap;
    private long _lastCacheTime = 0L;
    private long _maxCacheTime = 60 * 60 * 1000L;

    public void SetCache(Dictionary<string, ReceiveNetworkDto> receiveNetworkMap, Dictionary<string, NetworkInfoDto> networkMap,
        Dictionary<string, SendNetworkDto> sendEBridgeMap)
    {
        _receiveNetworkMap = receiveNetworkMap;
        _networkMap = networkMap;
        _sendEBridgeMap = sendEBridgeMap;
        _lastCacheTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
                result = new ReceiveNetworkDto { DestinationMap = new Dictionary<string, List<NetworkInfoDto>>() };
                result.DestinationMap[request.ChainId] = new List<NetworkInfoDto>
                {
                    ShiftChainHelper.GetAELFInfo(request.ChainId)
                };
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
}
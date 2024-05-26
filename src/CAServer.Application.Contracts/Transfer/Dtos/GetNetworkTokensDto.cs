using System.Collections.Generic;

namespace CAServer.Transfer.Dtos;

public class GetNetworkTokensDto
{
    public List<NetworkTokenInfo> TokenList { get; set; } = new();
}

public class NetworkTokenInfo
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string ContractAddress { get; set; }
    public string Icon { get; set; }
    public List<NetworkDto> NetworkList { get; set; }
}
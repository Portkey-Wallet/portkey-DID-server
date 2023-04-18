using System.Collections.Generic;

namespace CAServer.UserAssets.Dtos;

public class GetNFTProtocolsDto
{
    public List<NftProtocol> Data { get; set; }
}

public class NftProtocol
{
    public string ImageUrl { get; set; }
    public string ProtocolName { get; set; }
    public int ItemCount { get; set; }
    public string ChainId { get; set; }
    public string Symbol { get; set; }
    public string NftType { get; set; }
}
namespace CAServer.Transfer.Dtos;

public class GetNetworkTokensRequestDto
{
    public string Type { get; set; }
    public string Network { get; set; }
    public string ChainId { get; set; }
}
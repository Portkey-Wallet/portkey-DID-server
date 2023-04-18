namespace CAServer.Contract;

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string ContractAddress { get; set; }
    public string PrivateKey { get; set; }
    public bool IsMainChain { get; set; }
}
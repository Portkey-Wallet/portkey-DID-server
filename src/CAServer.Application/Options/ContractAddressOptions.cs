namespace CAServer.Options;

public class ContractAddressOptions
{
    public TokenClaimAddress TokenClaimAddress { get; set; }
}

public class TokenClaimAddress
{
    public string ContractName { get; set; }
    public string MainChainAddress { get; set; }
    public string SideChainAddress { get; set; }
}
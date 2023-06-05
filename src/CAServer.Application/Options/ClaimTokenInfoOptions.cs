namespace CAServer.Options;

public class ClaimTokenInfoOptions
{
    public string ChainId { get; set; }
    public string PublicKey { get; set; }
    public string ClaimTokenAddress { get; set; }
    public int ExpireTime { get; set; }
    public long ClaimTokenAmount { get; set; }
    
    public int GetClaimTokenLimit { get; set; }
}
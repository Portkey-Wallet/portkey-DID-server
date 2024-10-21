using TonProof.Types;

namespace CAServer.Verifier.Dtos;

public class TonWalletRequestDetail
{
    public string UserFriendlyAddress { get; set; }
    
    public CheckProofRequest Request { get; set; }
}
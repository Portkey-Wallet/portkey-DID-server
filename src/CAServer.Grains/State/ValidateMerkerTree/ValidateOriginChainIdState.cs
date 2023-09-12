using CAServer.ValidateMerkerTree;

namespace CAServer.Grains.State.ValidateMerkerTree;

public class ValidateOriginChainIdState
{
    public long LastUpdateTime { get; set; }
    public string TransactionId { get; set; }
    public string ChainId { get; set; }
    public ValidateStatus Status { get; set; }
}
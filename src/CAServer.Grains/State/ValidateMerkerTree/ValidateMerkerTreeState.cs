using CAServer.ValidateMerkerTree;

namespace CAServer.Grains.State.ValidateMerkerTree;

public class ValidateMerkerTreeState
{
    public long LastUpdateTime { get; set; }
    public string MerkleTreeRoot { get; set; }
    public string TransactionId { get; set; }
    public ValidateStatus Status { get; set; }
}
namespace CAServer.ValidateMerkerTree.Dtos;

public class ValidateMerkerTreeGrainDto
{
    public long LastUpdateTime { get; set; }
    public string MerkleTreeRoot { get; set; }
    public string TransactionId { get; set; }
    public string ChainId { get; set; }
    public ValidateStatus Status { get; set; }
}
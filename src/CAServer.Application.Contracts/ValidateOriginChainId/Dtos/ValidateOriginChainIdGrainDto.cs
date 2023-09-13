namespace CAServer.ValidateOriginChainId.Dtos;

public class ValidateOriginChainIdGrainDto
{
    public long LastUpdateTime { get; set; }
    public string TransactionId { get; set; }
    public string ChainId { get; set; }
    public ValidateStatus Status { get; set; }
}
using Orleans;

namespace CAServer.ValidateOriginChainId.Dtos;

[GenerateSerializer]
public class ValidateOriginChainIdGrainDto
{
    [Id(0)]
    public long LastUpdateTime { get; set; }
    [Id(1)]
    public string TransactionId { get; set; }
    [Id(2)]
    public string ChainId { get; set; }
    [Id(3)]
    public ValidateStatus Status { get; set; }
}
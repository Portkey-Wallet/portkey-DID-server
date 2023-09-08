using CAServer.ValidateMerkerTree.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.ValidateMerkerTree;

public interface IValidateMerkerTreeGrain : IGrainWithGuidKey
{
    Task<ValidateMerkerTreeGrainDto> GetInfoAsync();
    Task<bool> NeedValidateAsync();
    Task SetInfoAsync(string transactionId, string merkleTreeRoot);
}
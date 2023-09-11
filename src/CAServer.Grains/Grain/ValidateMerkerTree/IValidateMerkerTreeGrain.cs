using CAServer.ValidateMerkerTree.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.ValidateMerkerTree;

public interface IValidateMerkerTreeGrain : IGrainWithGuidKey
{
    Task SetStatusSuccessAsync();    
    Task SetStatusFailAsync();
    Task<GrainResultDto<ValidateMerkerTreeGrainDto>> GetInfoAsync();
    Task<GrainResultDto<bool>> NeedValidateAsync();
    Task SetInfoAsync(string transactionId, string merkleTreeRoot, string chainId);
}
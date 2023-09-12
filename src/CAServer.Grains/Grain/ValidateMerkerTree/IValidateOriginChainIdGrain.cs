using CAServer.ValidateMerkerTree.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.ValidateMerkerTree;

public interface IValidateOriginChainIdGrain : IGrainWithGuidKey
{
    Task SetStatusSuccessAsync();    
    Task SetStatusFailAsync();
    Task<GrainResultDto<ValidateOriginChainIdGrainDto>> GetInfoAsync();
    Task<GrainResultDto<bool>> NeedValidateAsync();
    Task SetInfoAsync(string transactionId,string chainId);
}
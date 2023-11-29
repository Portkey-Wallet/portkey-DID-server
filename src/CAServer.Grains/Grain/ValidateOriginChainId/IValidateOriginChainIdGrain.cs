using CAServer.ValidateOriginChainId.Dtos;
using Orleans;

namespace CAServer.Grains.Grain.ValidateOriginChainId;

public interface IValidateOriginChainIdGrain : IGrainWithGuidKey
{
    Task SetStatusSuccessAsync();    
    Task SetStatusFailAsync();
    Task<GrainResultDto<ValidateOriginChainIdGrainDto>> GetInfoAsync();
    Task<GrainResultDto<bool>> NeedValidateAsync();
    Task SetInfoAsync(string transactionId,string chainId);
}
using CAServer.Grains.State.ThirdPart;
using Orleans;

namespace CAServer.Grains.Grain.ThirdPart;

public interface ITransakAccessTokenGrain : IGrainWithStringKey
{

    Task<GrainResultDto<TransakAccessTokenDto>> GetAccessToken();
    
    Task<GrainResultDto<TransakAccessTokenDto>> SetAccessToken(TransakAccessTokenDto newAccessTokenState);
    
}
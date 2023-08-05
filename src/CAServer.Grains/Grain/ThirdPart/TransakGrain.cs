using CAServer.Grains.State.Thirdpart;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class TransakGrain : Grain<TransakAccessTokenState>, ITransakGrain
{
    
    private readonly IObjectMapper _objectMapper;

    public TransakGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }


    public Task<GrainResultDto<TransakAccessTokenDto>> GetAccessToken()
    {
        return Task.FromResult(new GrainResultDto<TransakAccessTokenDto>
        {
            Success = true,
            Data = _objectMapper.Map<TransakAccessTokenState, TransakAccessTokenDto>(State)
        });
    }

    public Task<GrainResultDto<TransakAccessTokenDto>> SetAccessToken(TransakAccessTokenDto newAccessToken)
    {
        var id = State.Id;
        var history = State.History.ToList();
        _objectMapper.Map(newAccessToken, State);
        if (State.Id.IsNullOrEmpty())
        {
            State.Id = id;
        }
        
        // save 10 latest AccessToken
        history.AddFirst(newAccessToken);
        State.History = history.Take(10).ToList();

        return Task.FromResult(new GrainResultDto<TransakAccessTokenDto>
        {
            Success = true,
            Data = _objectMapper.Map<TransakAccessTokenState, TransakAccessTokenDto>(State)
        });
    }
}
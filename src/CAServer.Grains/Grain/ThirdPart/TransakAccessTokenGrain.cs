using CAServer.Grains.State.ThirdPart;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class TransakAccessTokenGrain : Grain<TransakAccessTokenState>, ITransakAccessTokenGrain
{
    
    private readonly IObjectMapper _objectMapper;

    public TransakAccessTokenGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<TransakAccessTokenDto>> SetAccessToken(TransakAccessTokenDto newAccessToken)
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
        State.History = history.Take(2).ToList();
        
        await WriteStateAsync();
        
        return new GrainResultDto<TransakAccessTokenDto>
        {
            Success = true,
            Data = _objectMapper.Map<TransakAccessTokenState, TransakAccessTokenDto>(State)
        };
    }
}
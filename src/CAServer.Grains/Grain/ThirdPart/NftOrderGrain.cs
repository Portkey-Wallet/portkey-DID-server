using CAServer.Grains.State.Order;
using CAServer.ThirdPart;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ThirdPart;

public class NftOrderGrain : Grain<NftOrderState>, INftOrderGrain
{
    private readonly IObjectMapper _objectMapper;

    public NftOrderGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }


    public async Task<GrainResultDto<NftOrderGrainDto>> CreateNftOrder(NftOrderGrainDto input)
    {
        return State.NftSymbol.IsNullOrEmpty()
            ? await UpdateNftOrder(input)
            : new GrainResultDto<NftOrderGrainDto>().Error("NFT Order exists");
    }

    public async Task<GrainResultDto<NftOrderGrainDto>> UpdateNftOrder(NftOrderGrainDto input)
    {
        State = _objectMapper.Map<NftOrderGrainDto, NftOrderState>(input);
        State.Id = State.Id == Guid.Empty ? this.GetPrimaryKey() : State.Id;
        await WriteStateAsync();
        return new GrainResultDto<NftOrderGrainDto>(_objectMapper.Map<NftOrderState, NftOrderGrainDto>(State));
    }

    public Task<GrainResultDto<NftOrderGrainDto>> GetNftOrder()
    {
        return Task.FromResult(new GrainResultDto<NftOrderGrainDto>(State.Id == Guid.Empty
                ? null
                : _objectMapper.Map<NftOrderState, NftOrderGrainDto>(State)));
    }
}
using CAServer.Grains.Grain.Account;
using CAServer.Grains.State.Chain;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.Chain;

public class ChainGrain : Grain<ChainState>, IChainGrain
{
    private readonly IObjectMapper _objectMapper;

    public ChainGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<GrainResultDto<ChainGrainDto>> AddChainAsync(ChainGrainDto chainDto)
    {
        var result = new GrainResultDto<ChainGrainDto>();

        if (!string.IsNullOrWhiteSpace(State.ChainId) && !State.IsDeleted)
        {
            result.Message = ChainMessage.ExistedMessage;
            return result;
        }

        State.Id = this.GetPrimaryKeyString();
        State.ChainId = chainDto.ChainId;
        State.ChainName = chainDto.ChainName;
        State.EndPoint = chainDto.EndPoint;
        State.CaContractAddress = chainDto.CaContractAddress;
        State.ExplorerUrl = chainDto.ExplorerUrl;
        State.LastModifyTime = DateTime.UtcNow;
        State.DefaultToken = chainDto.DefaultToken;
        State.IsDeleted = false;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ChainState, ChainGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<ChainGrainDto>> UpdateChainAsync(ChainGrainDto chainDto)
    {
        var result = new GrainResultDto<ChainGrainDto>();

        if (string.IsNullOrWhiteSpace(State.ChainId) || State.IsDeleted)
        {
            result.Message = ChainMessage.NotExistMessage;
            return result;
        }

        State.Id = this.GetPrimaryKeyString();
        State.ChainName = chainDto.ChainName;
        State.EndPoint = chainDto.EndPoint;
        State.CaContractAddress = chainDto.CaContractAddress;
        State.ExplorerUrl = chainDto.ExplorerUrl;
        State.LastModifyTime = DateTime.UtcNow;
        State.DefaultToken = chainDto.DefaultToken;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ChainState, ChainGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<ChainGrainDto>> DeleteChainAsync()
    {
        var result = new GrainResultDto<ChainGrainDto>();

        if (string.IsNullOrWhiteSpace(State.ChainId) || State.IsDeleted)
        {
            result.Message = ChainMessage.NotExistMessage;
            return result;
        }

        State.IsDeleted = true;
        State.LastModifyTime = DateTime.UtcNow;

        await WriteStateAsync();

        result.Success = true;
        result.Data = _objectMapper.Map<ChainState, ChainGrainDto>(State);
        return result;
    }
}
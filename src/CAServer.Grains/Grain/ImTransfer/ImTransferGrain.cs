using CAServer.Grains.State.ImTransfer;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ImTransfer;

public class ImTransferGrain : Orleans.Grain<ImTransferState>, IImTransferGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ImTransferGrain> _logger;

    public ImTransferGrain(IObjectMapper objectMapper, ILogger<ImTransferGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
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

    public async Task<GrainResultDto<TransferGrainDto>> CreateTransfer(TransferGrainDto transferGrainDto)
    {
        var result = new GrainResultDto<TransferGrainDto>();
        if (!State.Id.IsNullOrWhiteSpace())
        {
            result.Message = "transfer already exists.";
            return result;
        }

        State = _objectMapper.Map<TransferGrainDto, ImTransferState>(transferGrainDto);
        State.Id = this.GetPrimaryKeyString();
        State.CreateTime = DateTimeOffset.UtcNow;
        State.ModificationTime = DateTimeOffset.UtcNow;
        
        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<ImTransferState, TransferGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<TransferGrainDto>> UpdateTransfer(TransferGrainDto transferGrainDto)
    {
        var result = new GrainResultDto<TransferGrainDto>();
        if (State.Id.IsNullOrWhiteSpace())
        {
            result.Message = "transfer not exists.";
            return result;
        }

        State = _objectMapper.Map<TransferGrainDto, ImTransferState>(transferGrainDto);
        State.Id = this.GetPrimaryKeyString();
        State.ModificationTime = DateTimeOffset.UtcNow;
        
        await WriteStateAsync();
        result.Success = true;
        result.Data = _objectMapper.Map<ImTransferState, TransferGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<TransferGrainDto>> GetTransfer()
    {
        var result = new GrainResultDto<TransferGrainDto>();
        if (State.Id.IsNullOrWhiteSpace())
        {
            result.Message = "transfer not exists.";
            return Task.FromResult(result);
        }
        
        result.Success = true;
        result.Data = _objectMapper.Map<ImTransferState, TransferGrainDto>(State);
        return Task.FromResult(result);
    }
}
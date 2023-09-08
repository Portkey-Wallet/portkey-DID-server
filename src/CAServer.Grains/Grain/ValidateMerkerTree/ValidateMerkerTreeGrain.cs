using CAServer.Grains.State.ValidateMerkerTree;
using CAServer.ValidateMerkerTree;
using CAServer.ValidateMerkerTree.Dtos;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ValidateMerkerTree;

public class ValidateMerkerTreeGrain : Orleans.Grain<ValidateMerkerTreeState>, IValidateMerkerTreeGrain
{
    private readonly IObjectMapper _objectMapper;
    
    public ValidateMerkerTreeGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<ValidateMerkerTreeGrainDto> GetInfoAsync()
    {
        return _objectMapper.Map<ValidateMerkerTreeState, ValidateMerkerTreeGrainDto>(State);
    }
    
    public async Task SetInfoAsync(string transactionId, string merkleTreeRoot)
    {
        State.TransactionId = transactionId;
        State.MerkleTreeRoot = merkleTreeRoot;
        State.Status = ValidateStatus.Processing;
        await WriteStateAsync();
    }
    
    public async Task<bool> NeedValidateAsync()
    {
        if (State.Status == ValidateStatus.Success)
        {
            return false;
        }

        if (State.Status == ValidateStatus.Init || State.Status == ValidateStatus.Fail)
        {
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - State.LastUpdateTime < ValidateConst.DefaultWaitTimeMs)
            {
                return false;
            }
            State.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            State.Status = ValidateStatus.Processing;
            await WriteStateAsync();
            return true;
        }
        
        if (State.Status == ValidateStatus.Processing)
        {
            if (string.IsNullOrWhiteSpace(State.TransactionId))
            {
                return false;
            }
            //todo:check transaction id result
            return false;
        }
        
        return false;
    }
}
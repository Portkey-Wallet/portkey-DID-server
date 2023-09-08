using AElf.Client.Service;
using AElf.Types;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ValidateMerkerTree;
using CAServer.ValidateMerkerTree;
using CAServer.ValidateMerkerTree.Dtos;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ValidateMerkerTree;

public class ValidateMerkerTreeGrain : Orleans.Grain<ValidateMerkerTreeState>, IValidateMerkerTreeGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ChainOptions _chainOptions;
    
    public ValidateMerkerTreeGrain(IObjectMapper objectMapper, IOptions<ChainOptions> chainOptions)
    {
        _objectMapper = objectMapper;
        _chainOptions = chainOptions.Value;
    }

    public async Task SetStatusFailAsync()
    {
        State.Status = ValidateStatus.Fail;
        await WriteStateAsync();
    }
    
    public async Task SetStatusSuccessAsync()
    {
        State.Status = ValidateStatus.Success;
        await WriteStateAsync();
    }
    
    public async Task<ValidateMerkerTreeGrainDto> GetInfoAsync()
    {
        return _objectMapper.Map<ValidateMerkerTreeState, ValidateMerkerTreeGrainDto>(State);
    }

    public async Task SetInfoAsync(string transactionId, string merkleTreeRoot, string chainId)
    {
        State.TransactionId = transactionId;
        State.MerkleTreeRoot = merkleTreeRoot;
        State.ChainId = chainId;
        State.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
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
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - State.LastUpdateTime < ValidateConst.ProcessingWaitTimeMs)
            {
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(State.TransactionId) || string.IsNullOrWhiteSpace(State.ChainId) ||
                string.IsNullOrWhiteSpace(State.MerkleTreeRoot))
            {
                //this means some error , we can sync again
                State.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return true;
            }
            
            if (!_chainOptions.ChainInfos.TryGetValue(State.ChainId, out var chainInfo))
            {
                return false;
            }
            
            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            
            var txResult =
                await client.GetTransactionResultAsync(State.TransactionId);
            if (txResult.Status == TransactionState.Mined)
            {
                State.Status = ValidateStatus.Success;
                await WriteStateAsync();
                return false;
            }
            
            State.Status = ValidateStatus.Fail;
            State.LastUpdateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await WriteStateAsync();
            return true;
        }
        
        return false;
    }
}
namespace CAServer.Grains.Grain.CrossChain;

public interface ICrossChainTransferGrain : IGrainWithStringKey
{
    Task<GrainResultDto<List<CrossChainTransferDto>>> GetUnFinishedTransfersAsync();
    Task<GrainResultDto<long>> GetLastedProcessedHeightAsync();
    Task AddTransfersAsync(long lastedHeight, List<CrossChainTransferDto> transfers);
    Task UpdateTransferAsync(CrossChainTransferDto transfer);
    Task<Dictionary<long,string>> GetTransactionDicAsync();
    Task UpdateTransfersDicAsync(long startHeight, Dictionary<long, string> dic);
}
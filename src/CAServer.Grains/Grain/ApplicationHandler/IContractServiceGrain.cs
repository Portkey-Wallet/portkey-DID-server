using AElf.Client.Dto;
using Google.Protobuf.Collections;
using Orleans;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface IContractServiceGrain : IGrainWithGuidKey
{
    Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);
    Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);

    Task<TransactionDto> ValidateTransactionAsync(string chainId, GetHolderInfoOutput output,
        RepeatedField<string> unsetLoginGuardianTypes);

    Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionDto transactionDto);
    Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input);
}
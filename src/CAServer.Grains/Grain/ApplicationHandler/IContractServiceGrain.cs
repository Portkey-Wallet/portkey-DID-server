using AElf.Client.Dto;
using Google.Protobuf.Collections;
using Orleans;
using Portkey.Contracts.CA;

namespace CAServer.Grains.Grain.ApplicationHandler;

public interface IContractServiceGrain : IGrainWithGuidKey
{
    Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);
    Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);

    Task<TransactionInfoDto> ValidateTransactionAsync(string chainId, GetHolderInfoOutput output,
        RepeatedField<string> unsetLoginGuardianTypes);

    Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfoDto transactionInfoDto);
    Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input);
}
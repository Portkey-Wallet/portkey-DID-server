// using AElf.Client.Dto;
// using CAServer.CAAccount;
// using CAServer.CAAccount.Dtos;
// using CAServer.Grains.State.ApplicationHandler;
// using Google.Protobuf;
// using Google.Protobuf.Collections;
// using Orleans;
// using Portkey.Contracts.CA;
//
// namespace CAServer.Grains.Grain.ApplicationHandler;
//
// public interface IContractServiceGrain : IGrainWithGuidKey
// {
//     Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);
//     Task<TransactionResultDto> CreateHolderInfoOnNonCreateChainAsync(string chainId, CreateHolderDto createHolderDto);
//     Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);
//
//     Task<TransactionInfoDto> ValidateTransactionAsync(string chainId, GetHolderInfoOutput output,
//         RepeatedField<string> unsetLoginGuardianTypes);
//
//     Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionInfo transactionInfo);
//     Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input);
//     Task<TransactionResultDto> ForwardTransactionAsync(string chainId, string rawTransaction);
//
//     Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(string chainId, IMessage param, string payRedPackageFrom,
//         string redPackageContractAddress,string methodName);
//
//     Task<TransactionResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeDto assignProjectDelegateeDto);
// }
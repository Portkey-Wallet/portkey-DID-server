// using AElf;
// using AElf.Client.Dto;
// using AElf.Client.Service;
// using AElf.Standards.ACS7;
// using AElf.Types;
// using CAServer.CAAccount;
// using CAServer.CAAccount.Dtos;
// using CAServer.Commons;
// using CAServer.Grains.State.ApplicationHandler;
// using CAServer.Monitor;
// using CAServer.Signature;
// using CAServer.Signature.Provider;
// using Google.Protobuf;
// using Google.Protobuf.Collections;
// using Google.Protobuf.WellKnownTypes;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Newtonsoft.Json;
// using Orleans.Concurrency;
// using Portkey.Contracts.CA;
// using Volo.Abp.ObjectMapping;
//
// namespace CAServer.Grains.Grain.ApplicationHandler;
//
// [StatelessWorker]
// public class ContractServiceGrain : Orleans.Grain, IContractServiceGrain
// {
//     private readonly GrainOptions _grainOptions;
//     private readonly ChainOptions _chainOptions;
//     private readonly IObjectMapper _objectMapper;
//     private readonly ILogger<ContractServiceGrain> _logger;
//     private readonly ISignatureProvider _signatureProvider;
//     private readonly IIndicatorScope _indicatorScope;
//
//     public ContractServiceGrain(IOptions<ChainOptions> chainOptions, IOptions<GrainOptions> grainOptions,
//         IObjectMapper objectMapper, ISignatureProvider signatureProvider, ILogger<ContractServiceGrain> logger, IIndicatorScope indicatorScope)
//     {
//         _objectMapper = objectMapper;
//         _logger = logger;
//         _indicatorScope = indicatorScope;
//         _grainOptions = grainOptions.Value;
//         _chainOptions = chainOptions.Value;
//         _signatureProvider = signatureProvider;
//     }
//
//     private async Task<TransactionInfoDto> SendTransactionToChainAsync(string chainId, IMessage param,
//         string methodName)
//     {
//         try
//         {
//             if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
//             {
//                 return null;
//             }
//
//             var client = new AElfClient(chainInfo.BaseUrl);
//             await client.IsConnectedAsync();
//             var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
//             _logger.LogDebug("Get Address From PubKey, ownAddress：{ownAddress}, ContractAddress: {ContractAddress} ",
//                 ownAddress, chainInfo.ContractAddress);
//
//             var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.GenerateTransactionAsync.ToString());
//             
//             var transaction =
//                 await client.GenerateTransactionAsync(ownAddress, chainInfo.ContractAddress, methodName,
//                     param);
//             _indicatorScope.End(interIndicator);
//             
//             var refBlockNumber = transaction.RefBlockNumber;
//
//             refBlockNumber -= _grainOptions.SafeBlockHeight;
//
//             if (refBlockNumber < 0)
//             {
//                 refBlockNumber = 0;
//             }
//
//             var blockDto = await client.GetBlockByHeightAsync(refBlockNumber);
//
//             transaction.RefBlockNumber = refBlockNumber;
//             transaction.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(Hash.LoadFromHex(blockDto.BlockHash));
//
//             var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());
//             _logger.LogDebug("signature provider sign result: {txWithSign}", txWithSign);
//             transaction.Signature = ByteStringHelper.FromHexString(txWithSign);
//
//             var sendIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.SendTransactionAsync.ToString());
//             var result = await client.SendTransactionAsync(new SendTransactionInput
//             {
//                 RawTransaction = transaction.ToByteArray().ToHex()
//             });
//             _indicatorScope.End(sendIndicator);
//
//             await Task.Delay(_grainOptions.Delay);
//
//             var getIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.GetTransactionResultAsync.ToString());
//             var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//             _indicatorScope.End(getIndicator);
//             
//             var times = 0;
//             while ((transactionResult.Status == TransactionState.Pending || transactionResult.Status == TransactionState.NotExisted) && times < _grainOptions.RetryTimes)
//             {
//                 times++;
//                 await Task.Delay(_grainOptions.RetryDelay);
//                 
//                 var retryGetIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                     MonitorAelfClientType.GetTransactionResultAsync.ToString());
//                 transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//                 
//                 _indicatorScope.End(retryGetIndicator);
//             }
//
//             return new TransactionInfoDto
//             {
//                 Transaction = transaction,
//                 TransactionResultDto = transactionResult
//             };
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, methodName + " error: {param}", param);
//             return new TransactionInfoDto();
//         }
//     }
//
//     private async Task<TransactionInfoDto> ForwardTransactionToChainAsync(string chainId, string rawTransaction)
//     {
//          try
//          {
//              if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
//              {
//                  return null;
//              }
//
//              var client = new AElfClient(chainInfo.BaseUrl);
//              await client.IsConnectedAsync();
//
//              var result = await client.SendTransactionAsync(new SendTransactionInput
//              {
//                  RawTransaction = rawTransaction
//              });
//
//              await Task.Delay(_grainOptions.Delay);
//
//              var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//             
//             var times = 0;
//             while ((transactionResult.Status == TransactionState.Pending || transactionResult.Status == TransactionState.NotExisted) && times < _grainOptions.CryptoBoxRetryTimes)
//             {
//                 times++;
//                 await Task.Delay(_grainOptions.CryptoBoxRetryDelay);
//                 transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//             }
//
//              return new TransactionInfoDto
//              {
//                  TransactionResultDto = transactionResult
//              };
//          }
//          catch (Exception e)
//          {
//              _logger.LogError(e, "ForwardTransactionToChainAsync error,chain:{chain}", chainId);
//              return new TransactionInfoDto();
//          }
//     }
//
//     public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
//     {
//         var param = _objectMapper.Map<CreateHolderDto, CreateCAHolderInput>(createHolderDto);
//
//         var result = await SendTransactionToChainAsync(createHolderDto.ChainId, param, MethodName.CreateCAHolder);
//         
//         DeactivateOnIdle();
//         
//         return result.TransactionResultDto;
//     }
//
//     public async Task<TransactionResultDto> CreateHolderInfoOnNonCreateChainAsync(string chainId,
//         CreateHolderDto createHolderDto)
//     {
//         var param = _objectMapper.Map<CreateHolderDto, ReportPreCrossChainSyncHolderInfoInput>(createHolderDto);
//         param.CreateChainId = ChainHelper.ConvertBase58ToChainId(createHolderDto.ChainId);
//         param.CaHash = createHolderDto.CaHash;
//         var result = await SendTransactionToChainAsync(chainId, param, MethodName.CreateCAHolderOnNonCreateChain);
//         DeactivateOnIdle();
//         return result.TransactionResultDto;
//     }
//
//     public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
//     {
//         var param = _objectMapper.Map<SocialRecoveryDto, SocialRecoveryInput>(socialRecoveryDto);
//
//         var result = await SendTransactionToChainAsync(socialRecoveryDto.ChainId, param, MethodName.SocialRecovery);
//         
//         DeactivateOnIdle();
//         
//         return result.TransactionResultDto;
//     }
//
//     public async Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
//         GetHolderInfoOutput output, RepeatedField<string> unsetLoginGuardians)
//     {
//         var param = _objectMapper.Map<GetHolderInfoOutput, ValidateCAHolderInfoWithManagerInfosExistsInput>(output);
//
//         if (unsetLoginGuardians != null)
//         {
//             foreach (var notLoginGuardian in unsetLoginGuardians)
//             {
//                 param.NotLoginGuardians.Add(Hash.LoadFromHex(notLoginGuardian));
//             }
//         }
//
//         var result = await SendTransactionToChainAsync(chainId, param, MethodName.Validate);
//         
//         DeactivateOnIdle();
//
//         return result;
//     }
//
//     public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId,
//         TransactionInfo transactionInfo)
//     {
//         try
//         {
//             var chainInfo = _chainOptions.ChainInfos[chainId];
//             var client = new AElfClient(chainInfo.BaseUrl);
//             await client.IsConnectedAsync();
//
//             var syncHolderInfoInput = new SyncHolderInfoInput();
//
//             var validateTokenHeight = transactionInfo.BlockNumber;
//
//             var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.GetMerklePathByTransactionIdAsync.ToString());
//             
//             var merklePathDto =
//                 await client.GetMerklePathByTransactionIdAsync(transactionInfo.TransactionId);
//             _indicatorScope.End(interIndicator);
//             
//             var merklePath = new MerklePath();
//             foreach (var node in merklePathDto.MerklePathNodes)
//             {
//                 merklePath.MerklePathNodes.Add(new MerklePathNode
//                 {
//                     Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
//                     IsLeftChildNode = node.IsLeftChildNode
//                 });
//             }
//
//             var verificationTransactionInfo = new VerificationTransactionInfo
//             {
//                 FromChainId = ChainHelper.ConvertBase58ToChainId(chainId),
//                 MerklePath = merklePath,
//                 ParentChainHeight = validateTokenHeight,
//                 TransactionBytes = ByteString.CopyFrom(transactionInfo.Transaction)
//             };
//
//             syncHolderInfoInput.VerificationTransactionInfo = verificationTransactionInfo;
//
//             if (!chainInfo.IsMainChain)
//             {
//                 syncHolderInfoInput = await UpdateMerkleTreeAsync(chainId, client, syncHolderInfoInput);
//             }
//             
//             DeactivateOnIdle();
//
//             return syncHolderInfoInput;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "GetSyncHolderInfoInput error: ");
//             
//             DeactivateOnIdle();
//             
//             return new SyncHolderInfoInput();
//         }
//     }
//
//     private async Task<SyncHolderInfoInput> UpdateMerkleTreeAsync(string chainId, AElfClient client,
//         SyncHolderInfoInput syncHolderInfoInput)
//     {
//         try
//         {
//             var chainInfo = _chainOptions.ChainInfos[chainId];
//
//             var ownAddress = client.GetAddressFromPubKey(chainInfo.PublicKey);
//             var interIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.GenerateTransactionAsync.ToString());
//             
//             var transaction = await client.GenerateTransactionAsync(ownAddress, chainInfo.CrossChainContractAddress,
//                 MethodName.UpdateMerkleTree,
//                 new Int64Value
//                 {
//                     Value = syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight
//                 });
//             _indicatorScope.End(interIndicator);
//             
//             var txWithSign = await _signatureProvider.SignTxMsg(ownAddress, transaction.GetHash().ToHex());
//             transaction.Signature = ByteStringHelper.FromHexString(txWithSign);
//
//             var executeIndicator = _indicatorScope.Begin(MonitorTag.AelfClient,
//                 MonitorAelfClientType.ExecuteTransactionAsync.ToString());
//             
//             var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
//             {
//                 RawTransaction = transaction.ToByteArray().ToHex()
//             });
//             _indicatorScope.End(executeIndicator);
//             var context = CrossChainMerkleProofContext.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
//
//             syncHolderInfoInput.VerificationTransactionInfo.MerklePath.MerklePathNodes.AddRange(
//                 context.MerklePathFromParentChain.MerklePathNodes);
//
//             syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight = context.BoundParentChainHeight;
//
//             return syncHolderInfoInput;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "UpdateMerkleTree error, syncHolderInfoInput: {info}",
//                 JsonConvert.SerializeObject(syncHolderInfoInput.VerificationTransactionInfo.ToString()));
//             return new SyncHolderInfoInput();
//         }
//     }
//
//     public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
//     {
//         var result = await SendTransactionToChainAsync(chainId, input, MethodName.SyncHolderInfo);
//         
//         DeactivateOnIdle();
//
//         return result.TransactionResultDto;
//     }
//
//     public async Task<TransactionResultDto> ForwardTransactionAsync(string chainId, string rawTransaction)
//     {
//         try
//         {
//             // var chainInfo = _chainOptions.ChainInfos[chainId];
//             // var client = new AElfClient(chainInfo.BaseUrl);
//             // await client.IsConnectedAsync();
//
//             var result = await ForwardTransactionToChainAsync(chainId,rawTransaction);
//             DeactivateOnIdle();
//
//             return result.TransactionResultDto;
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, "ForwardTransactionAsync error: ");
//             
//             DeactivateOnIdle();
//             
//             return new TransactionResultDto();
//         }
//     }
//     
//         public async Task<TransactionInfoDto> SendTransferRedPacketToChainAsync(string chainId, IMessage param,
//             string payRedPackageFrom, string redPackageContractAddress,string methodName)
//     {
//         try
//         {
//             _logger.LogInformation("SendTransferRedPacketToChainAsync param: {param}", JsonConvert.SerializeObject(param));
//
//             if (!_chainOptions.ChainInfos.TryGetValue(chainId, out var chainInfo))
//             {
//                 return null;
//             }
//
//             var client = new AElfClient(chainInfo.BaseUrl);
//             await client.IsConnectedAsync();
//             var ownAddress = client.GetAddressFromPubKey(payRedPackageFrom); //select public key
//             _logger.LogInformation("Get Address From PubKey, ownAddress：{ownAddress}, ContractAddress: {ContractAddress} ,methodName:{methodName}",
//                 ownAddress, redPackageContractAddress, methodName);
//
//             //"red package contract address"
//             var transaction =
//                 await client.GenerateTransactionAsync(ownAddress,redPackageContractAddress , methodName,
//                     param);
//
//             var refBlockNumber = transaction.RefBlockNumber;
//
//             refBlockNumber -= _grainOptions.SafeBlockHeight;
//
//             if (refBlockNumber < 0)
//             {
//                 refBlockNumber = 0;
//             }
//
//             var blockDto = await client.GetBlockByHeightAsync(refBlockNumber);
//
//             transaction.RefBlockNumber = refBlockNumber;
//             transaction.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(Hash.LoadFromHex(blockDto.BlockHash));
//
//             var txWithSign = await _signatureProvider.SignTxMsg(payRedPackageFrom, transaction.GetHash().ToHex());
//             _logger.LogInformation("signature provider sign result: {txWithSign}", txWithSign);
//             transaction.Signature = ByteStringHelper.FromHexString(txWithSign);
//
//             var result = await client.SendTransactionAsync(new SendTransactionInput
//             {
//                 RawTransaction = transaction.ToByteArray().ToHex()
//             });
//             _logger.LogInformation("SendTransferRedPacketToChainAsync result: {result}", JsonConvert.SerializeObject(result));
//
//             await Task.Delay(_grainOptions.Delay);
//
//             var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//             _logger.LogInformation("SendTransferRedPacketToChainAsync transactionResult: {transactionResult}", JsonConvert.SerializeObject(transactionResult));
//
//             var times = 0;
//             while ((transactionResult.Status == TransactionState.Pending || transactionResult.Status == TransactionState.NotExisted) && times < _grainOptions.RetryTimes)
//             {
//                 times++;
//                 await Task.Delay(_grainOptions.RetryDelay);
//
//                 transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
//             }
//
//             return new TransactionInfoDto
//             {
//                 Transaction = transaction,
//                 TransactionResultDto = transactionResult
//             };
//         }
//         catch (Exception e)
//         {
//             _logger.LogError(e, methodName + " error: {param}", param);
//             return new TransactionInfoDto();
//         }
//     }
//
//     public async Task<TransactionResultDto> AuthorizeDelegateAsync(AssignProjectDelegateeDto assignProjectDelegateeDto)
//         {
//             var param = _objectMapper.Map<AssignProjectDelegateeDto, AssignProjectDelegateeInput>(assignProjectDelegateeDto);
//             var result = await SendTransactionToChainAsync(assignProjectDelegateeDto.ChainId, param, MethodName.AssignProjectDelegatee);
//             DeactivateOnIdle();
//             return result.TransactionResultDto;
//         }
// }
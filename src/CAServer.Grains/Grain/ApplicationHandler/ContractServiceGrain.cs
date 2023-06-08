using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Concurrency;
using Portkey.Contracts.CA;
using Volo.Abp.ObjectMapping;

namespace CAServer.Grains.Grain.ApplicationHandler;

[StatelessWorker]
public class ContractServiceGrain : Orleans.Grain, IContractServiceGrain
{
    private readonly GrainOptions _grainOptions;
    private readonly ChainOptions _chainOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ContractServiceGrain> _logger;

    public ContractServiceGrain(IOptions<ChainOptions> chainOptions, IOptions<GrainOptions> grainOptions,
        IObjectMapper objectMapper, ILogger<ContractServiceGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _grainOptions = grainOptions.Value;
        _chainOptions = chainOptions.Value;
    }

    private async Task<TransactionInfoDto> SendTransactionToChainAsync(string chainId, IMessage param,
        string methodName)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];
            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, chainInfo.ContractAddress, methodName,
                    param);

            var refBlockNumber = transaction.RefBlockNumber;

            refBlockNumber -= _grainOptions.SafeBlockHeight;

            if (refBlockNumber < 0)
            {
                refBlockNumber = 0;
            }

            var blockDto = await client.GetBlockByHeightAsync(refBlockNumber);

            transaction.RefBlockNumber = refBlockNumber;
            transaction.RefBlockPrefix = BlockHelper.GetRefBlockPrefix(Hash.LoadFromHex(blockDto.BlockHash));

            var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

            var result = await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            await Task.Delay(_grainOptions.Delay);

            var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);

            var times = 0;
            while (transactionResult.Status == TransactionState.Pending && times < _grainOptions.RetryTimes)
            {
                times++;
                await Task.Delay(_grainOptions.RetryDelay);
                transactionResult = await client.GetTransactionResultAsync(result.TransactionId);

                if (transactionResult.Status != TransactionState.Pending)
                {
                    _logger.LogError($"#### status: {transactionResult.Status}, times: {times}");
                }
            }

            return new TransactionInfoDto
            {
                Transaction = transaction,
                TransactionResultDto = transactionResult
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, methodName + " error: {param}", param);
            return new TransactionInfoDto();
        }
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        var param = _objectMapper.Map<CreateHolderDto, CreateCAHolderInput>(createHolderDto);

        var result = await SendTransactionToChainAsync(createHolderDto.ChainId, param, MethodName.CreateCAHolder);
        return result.TransactionResultDto;
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        var param = _objectMapper.Map<SocialRecoveryDto, SocialRecoveryInput>(socialRecoveryDto);

        var result = await SendTransactionToChainAsync(socialRecoveryDto.ChainId, param, MethodName.SocialRecovery);
        return result.TransactionResultDto;
    }

    public async Task<TransactionInfoDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput output, RepeatedField<string> unsetLoginGuardians)
    {
        var param = _objectMapper.Map<GetHolderInfoOutput, ValidateCAHolderInfoWithManagerInfosExistsInput>(output);

        if (unsetLoginGuardians != null)
        {
            foreach (var notLoginGuardian in unsetLoginGuardians)
            {
                param.NotLoginGuardians.Add(Hash.LoadFromHex(notLoginGuardian));
            }
        }

        var result = await SendTransactionToChainAsync(chainId, param, MethodName.Validate);

        return result;
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId,
        TransactionInfoDto transactionInfoDto)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];
            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();

            var syncHolderInfoInput = new SyncHolderInfoInput();

            var validateTokenHeight = transactionInfoDto.TransactionResultDto.BlockNumber;

            var merklePathDto =
                await client.GetMerklePathByTransactionIdAsync(transactionInfoDto.TransactionResultDto.TransactionId);
            var merklePath = new MerklePath();
            foreach (var node in merklePathDto.MerklePathNodes)
            {
                merklePath.MerklePathNodes.Add(new MerklePathNode
                {
                    Hash = new Hash { Value = Hash.LoadFromHex(node.Hash).Value },
                    IsLeftChildNode = node.IsLeftChildNode
                });
            }

            var verificationTransactionInfo = new VerificationTransactionInfo
            {
                FromChainId = ChainHelper.ConvertBase58ToChainId(chainId),
                MerklePath = merklePath,
                ParentChainHeight = validateTokenHeight,
                TransactionBytes = transactionInfoDto.Transaction.ToByteString()
            };

            syncHolderInfoInput.VerificationTransactionInfo = verificationTransactionInfo;

            if (!chainInfo.IsMainChain)
            {
                syncHolderInfoInput = await UpdateMerkleTreeAsync(chainId, client, syncHolderInfoInput);
            }

            return syncHolderInfoInput;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncHolderInfoInput error: ");
            return new SyncHolderInfoInput();
        }
    }

    private async Task<SyncHolderInfoInput> UpdateMerkleTreeAsync(string chainId, AElfClient client,
        SyncHolderInfoInput syncHolderInfoInput)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);

            var transaction = await client.GenerateTransactionAsync(ownAddress, chainInfo.CrossChainContractAddress,
                MethodName.UpdateMerkleTree,
                new Int64Value
                {
                    Value = syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight
                });
            var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var context = CrossChainMerkleProofContext.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));

            syncHolderInfoInput.VerificationTransactionInfo.MerklePath.MerklePathNodes.AddRange(
                context.MerklePathFromParentChain.MerklePathNodes);

            syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight = context.BoundParentChainHeight;

            return syncHolderInfoInput;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "UpdateMerkleTree error, syncHolderInfoInput: {info}",
                JsonConvert.SerializeObject(syncHolderInfoInput.VerificationTransactionInfo.ToString()));
            return new SyncHolderInfoInput();
        }
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        var result = await SendTransactionToChainAsync(chainId, input, MethodName.SyncHolderInfo);

        return result.TransactionResultDto;
    }
}
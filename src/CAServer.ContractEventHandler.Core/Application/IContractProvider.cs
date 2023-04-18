using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Grains.Grain.ApplicationHandler;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Orleans;
using TransactionDto = CAServer.Grains.Grain.ApplicationHandler.TransactionDto;

namespace CAServer.ContractEventHandler.Core.Application;

public interface IContractProvider
{
    public Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId,
        string loginGuardianAccount, string caHash);

    public Task<int> GetChainIdAsync(string chainId);
    public Task<long> GetBlockHeightAsync(string chainId);
    public Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId);
    public Task<long> GetIndexHeightFromSideChainAsync(string sideChainId);
    public Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId);
    public Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto);
    public Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto);

    public Task<TransactionDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardianAccounts);

    public Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionDto transactionDto);

    public Task<TransactionResultDto> SyncTransactionAsync(string chainId,
        SyncHolderInfoInput syncHolderInfoInput);
}

public class ContractProvider : IContractProvider
{
    private readonly ILogger<ContractProvider> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly ChainOptions _chainOptions;

    public ContractProvider(ILogger<ContractProvider> logger, IOptions<ChainOptions> chainOptions,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _chainOptions = chainOptions.Value;
        _clusterClient = clusterClient;
    }

    private async Task<T> CallTransactionAsync<T>(string chainId, string methodName, IMessage param,
        bool isCrossChain) where T : IMessage<T>, new()
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);
            var contractAddress = isCrossChain
                ? (await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(ContractName.CrossChain)))
                .ToBase58()
                : chainInfo.ContractAddress;

            var transaction =
                await client.GenerateTransactionAsync(ownAddress, contractAddress,
                    methodName, param);
            var txWithSign = client.SignTransaction(chainInfo.PrivateKey, transaction);

            var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = txWithSign.ToByteArray().ToHex()
            });

            var value = new T();
            value.MergeFrom(ByteArrayHelper.HexStringToByteArray(result));

            return value;
        }
        catch (Exception e)
        {
            if (methodName != MethodName.GetHolderInfo)
            {
                _logger.LogError(e, methodName + " error: {param}", param);
            }

            return new T();
        }
    }

    public async Task<GetHolderInfoOutput> GetHolderInfoFromChainAsync(string chainId, string loginGuardianAccount,
        string caHash)
    {
        var param = new GetHolderInfoInput
        {
            CaHash = null,
            LoginGuardianAccount = loginGuardianAccount
        };

        if (caHash != null)
        {
            param.CaHash = Hash.LoadFromHex(caHash);
        }

        var output =
            await CallTransactionAsync<GetHolderInfoOutput>(chainId, MethodName.GetHolderInfo, param, false);

        if (output == null || output.CaHash.IsNullOrEmpty())
        {
            _logger.LogInformation(MethodName.GetHolderInfo + ": Empty result");
            return new GetHolderInfoOutput();
        }

        _logger.LogInformation(MethodName.GetHolderInfo + " result: {output}",
            JsonConvert.SerializeObject(output.ToString(), Formatting.Indented));

        return output;
    }

    public async Task<int> GetChainIdAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetChainIdAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetChainId on chain: {id} error", chainId);
            return ContractAppServiceConstant.IntError;
        }
    }

    public async Task<long> GetBlockHeightAsync(string chainId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            await client.IsConnectedAsync();
            return await client.GetBlockHeightAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBlockHeight on chain: {id} error", chainId);
            return ContractAppServiceConstant.LongError;
        }
    }

    public async Task<TransactionResultDto> GetTransactionResultAsync(string chainId, string txId)
    {
        try
        {
            var chainInfo = _chainOptions.ChainInfos[chainId];

            var client = new AElfClient(chainInfo.BaseUrl);
            return await client.GetTransactionResultAsync(txId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetTransactionResult on chain {chainId} with txId: {txId} error", chainId, txId);
            return new TransactionResultDto();
        }
    }

    public async Task<long> GetIndexHeightFromSideChainAsync(string sideChainId)
    {
        var result =
            await CallTransactionAsync<Int64Value>(sideChainId, MethodName.GetParentChainHeight, new Empty(), true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogInformation(MethodName.GetParentChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<long> GetIndexHeightFromMainChainAsync(string chainId, int sideChainId)
    {
        var param = new Int32Value
        {
            Value = sideChainId
        };

        var result = await CallTransactionAsync<Int64Value>(chainId, MethodName.GetSideChainHeight, param, true);
        if (result != null)
        {
            return result.Value;
        }

        _logger.LogInformation(MethodName.GetSideChainHeight + ": Empty result");
        return ContractAppServiceConstant.LongError;
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result =
                await grain.CreateHolderInfoAsync(createHolderDto);

            _logger.LogInformation(
                "CreateHolderInfo to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                createHolderDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateHolderInfo error: {message}",
                JsonConvert.SerializeObject(createHolderDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result = await grain.SocialRecoveryAsync(socialRecoveryDto);

            _logger.LogInformation(
                "SocialRecovery to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                socialRecoveryDto.ChainId,
                result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SocialRecovery error: {message}",
                JsonConvert.SerializeObject(socialRecoveryDto.ToString(), Formatting.Indented));
            return new TransactionResultDto
            {
                Status = TransactionState.Failed,
                Error = e.Message
            };
        }
    }

    public async Task<TransactionDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput result, RepeatedField<string> unsetLoginGuardianAccounts)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var transactionDto =
                await grain.ValidateTransactionAsync(chainId, result, unsetLoginGuardianAccounts);

            _logger.LogInformation(
                "ValidateTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId,
                transactionDto.TransactionResultDto.TransactionId, transactionDto.TransactionResultDto.BlockNumber,
                transactionDto.TransactionResultDto.Status,
                transactionDto.TransactionResultDto.Error);

            return transactionDto;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ValidateTransaction on chain: {id} error", chainId);
            return new TransactionDto();
        }
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionDto transactionDto)
    {
        try
        {
            if (transactionDto.TransactionResultDto == null || transactionDto.Transaction == null)
            {
                return new SyncHolderInfoInput();
            }

            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var syncHolderInfoInput =
                await grain.GetSyncHolderInfoInputAsync(chainId, transactionDto);

            _logger.LogInformation("GetSyncHolderInfoInput on chain {id} succeed", chainId);

            return syncHolderInfoInput;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSyncHolderInfoInput on chain: {id} error: {dto}", chainId,
                JsonConvert.SerializeObject(transactionDto.TransactionResultDto ?? new TransactionResultDto(),
                    Formatting.Indented));
            return new SyncHolderInfoInput();
        }
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId,
        SyncHolderInfoInput input)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGrain>(Guid.NewGuid());
            var result = await grain.SyncTransactionAsync(chainId, input);

            _logger.LogInformation(
                "SyncTransaction to chain: {id} result:" +
                "\nTransactionId: {transactionId}, BlockNumber: {number}, Status: {status}, ErrorInfo: {error}",
                chainId, result.TransactionId, result.BlockNumber, result.Status, result.Error);

            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SyncTransaction to Chain: {id} Error: {input}", chainId,
                JsonConvert.SerializeObject(input.ToString(), Formatting.Indented));
            return new TransactionResultDto();
        }
    }
}
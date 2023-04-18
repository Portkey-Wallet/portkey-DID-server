using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Orleans.Concurrency;

namespace CAServer.Grains.Grain.ApplicationHandler;

[StatelessWorker]
public class ContractServiceGrain : Orleans.Grain, IContractServiceGrain
{
    private readonly GrainOptions _grainOptions;
    private readonly ChainOptions _chainOptions;

    public ContractServiceGrain(IOptions<ChainOptions> chainOptions, IOptions<GrainOptions> grainOptions)
    {
        _grainOptions = grainOptions.Value;
        _chainOptions = chainOptions.Value;
    }

    private async Task<TransactionDto> SendTransactionToChainAsync(string chainId, IMessage param, string methodName)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();
        var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);

        var transaction =
            await client.GenerateTransactionAsync(ownAddress, chainInfo.ContractAddress, methodName,
                param);
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
        }

        return new TransactionDto
        {
            Transaction = transaction,
            TransactionResultDto = transactionResult
        };
    }

    public async Task<TransactionResultDto> CreateHolderInfoAsync(CreateHolderDto createHolderDto)
    {
        var param = new CreateCAHolderInput
        {
            GuardianApproved = new GuardianAccountInfo
            {
                Value = createHolderDto.GuardianAccountInfo.Value,
                Type = createHolderDto.GuardianAccountInfo.Type,
                VerificationInfo = new VerificationInfo
                {
                    Id = createHolderDto.GuardianAccountInfo.VerificationInfo.Id,
                    Signature = createHolderDto.GuardianAccountInfo.VerificationInfo.Signature,
                    VerificationDoc = createHolderDto.GuardianAccountInfo.VerificationInfo.VerificationDoc
                }
            },
            Manager = new Manager
            {
                ManagerAddress = createHolderDto.Manager.ManagerAddress,
                DeviceString = createHolderDto.Manager.DeviceString
            }
        };

        var result = await SendTransactionToChainAsync(createHolderDto.ChainId, param, MethodName.CreateCAHolder);
        return result.TransactionResultDto;
    }

    public async Task<TransactionResultDto> SocialRecoveryAsync(SocialRecoveryDto socialRecoveryDto)
    {
        var param = new SocialRecoveryInput
        {
            LoginGuardianAccount = socialRecoveryDto.LoginGuardianAccount,
            Manager = new Manager
            {
                DeviceString = socialRecoveryDto.Manager.DeviceString,
                ManagerAddress = socialRecoveryDto.Manager.ManagerAddress
            }
        };

        foreach (var guardian in socialRecoveryDto.GuardianApproved)
        {
            param.GuardiansApproved.Add(new GuardianAccountInfo
            {
                Value = guardian.Value,
                Type = guardian.Type,
                VerificationInfo = new VerificationInfo
                {
                    Id = guardian.VerificationInfo.Id,
                    Signature = guardian.VerificationInfo.Signature,
                    VerificationDoc = guardian.VerificationInfo.VerificationDoc
                }
            });
        }

        var result = await SendTransactionToChainAsync(socialRecoveryDto.ChainId, param, MethodName.SocialRecovery);
        return result.TransactionResultDto;
    }

    public async Task<TransactionDto> ValidateTransactionAsync(string chainId,
        GetHolderInfoOutput output, RepeatedField<string> unsetLoginGuardianAccounts)
    {
        var list = new RepeatedField<string>();
        foreach (var index in output.GuardiansInfo.LoginGuardianAccountIndexes)
        {
            list.Add(output.GuardiansInfo.GuardianAccounts[index].Value);
        }

        var param = new ValidateCAHolderInfoWithManagersExistsInput
        {
            CaHash = output.CaHash,
            Managers = { output.Managers },
            LoginGuardianAccounts = { list }
        };

        if (unsetLoginGuardianAccounts != null)
        {
            foreach (var notLoginGuardianAccount in unsetLoginGuardianAccounts)
            {
                param.NotLoginGuardianAccounts.Add(notLoginGuardianAccount);
            }
        }

        var result = await SendTransactionToChainAsync(chainId, param, MethodName.Validate);

        return result;
    }

    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsync(string chainId, TransactionDto transactionDto)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        var syncHolderInfoInput = new SyncHolderInfoInput();

        var validateTokenHeight = transactionDto.TransactionResultDto.BlockNumber;

        var merklePathDto =
            await client.GetMerklePathByTransactionIdAsync(transactionDto.TransactionResultDto.TransactionId);
        var merklePath = new MerklePath();
        foreach (var node in merklePathDto.MerklePathNodes)
        {
            merklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = Hash.LoadFromHex(node.Hash),
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        var verificationTransactionInfo = new VerificationTransactionInfo
        {
            FromChainId = ChainHelper.ConvertBase58ToChainId(chainId),
            MerklePath = merklePath,
            ParentChainHeight = validateTokenHeight,
            TransactionBytes = transactionDto.Transaction.ToByteString()
        };

        syncHolderInfoInput.VerificationTransactionInfo = verificationTransactionInfo;

        if (chainInfo.IsMainChain)
        {
            return syncHolderInfoInput;
        }

        return await UpdateMerkleTreeAsync(chainId, syncHolderInfoInput);
    }

    private async Task<SyncHolderInfoInput> UpdateMerkleTreeAsync(string chainId,
        SyncHolderInfoInput syncHolderInfoInput)
    {
        var chainInfo = _chainOptions.ChainInfos[chainId];
        var client = new AElfClient(chainInfo.BaseUrl);
        await client.IsConnectedAsync();

        var ownAddress = client.GetAddressFromPrivateKey(chainInfo.PrivateKey);

        var address =
            await client.GetContractAddressByNameAsync(HashHelper.ComputeFrom(ContractName.CrossChain));

        var transaction = await client.GenerateTransactionAsync(ownAddress, address.ToBase58(),
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

        foreach (var node in context.MerklePathFromParentChain.MerklePathNodes)
        {
            syncHolderInfoInput.VerificationTransactionInfo.MerklePath.MerklePathNodes.Add(new MerklePathNode
            {
                Hash = node.Hash,
                IsLeftChildNode = node.IsLeftChildNode
            });
        }

        syncHolderInfoInput.VerificationTransactionInfo.ParentChainHeight = context.BoundParentChainHeight;

        return syncHolderInfoInput;
    }

    public async Task<TransactionResultDto> SyncTransactionAsync(string chainId, SyncHolderInfoInput input)
    {
        var result = await SendTransactionToChainAsync(chainId, input, MethodName.SyncHolderInfo);

        return result.TransactionResultDto;
    }
}
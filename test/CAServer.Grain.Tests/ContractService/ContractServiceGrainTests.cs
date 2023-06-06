using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.State.ApplicationHandler;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Portkey.Contracts.CA;
using Xunit;

namespace CAServer.Grain.Tests.ContractService;

public  class ContractServiceGrainTests : CAServerGrainTestBase
{
    [Fact]
    public async Task CreateHolderInfoAsyncTests()
    {
        var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        await grain.CreateHolderInfoAsync(new CreateHolderDto
        {
            ChainId = "AELF",
            GuardianInfo = new GuardianInfo
            {
                Type = GuardianType.OfEmail,
                IdentifierHash = HashHelper.ComputeFrom("G"),
                VerificationInfo = new VerificationInfo
                {
                    Id = HashHelper.ComputeFrom("V"),
                    Signature = ByteString.Empty,
                    VerificationDoc = "doc"
                }
            },
            ManagerInfo = new ManagerInfo
            {
                Address = Address.FromPublicKey("AAA".HexToByteArray()),
                ExtraData = "extra"
            }
        });
    }

    [Fact]
    public async Task SocialRecoveryAsyncTests()
    {
        var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        await grain.SocialRecoveryAsync(new SocialRecoveryDto());
    }

    [Fact]
    public async Task ValidateTransactionAsyncTests()
    {
        var unsetList = new RepeatedField<string>();
        unsetList.Add(HashHelper.ComputeFrom("unset").ToHex());

        var managers = new RepeatedField<ManagerInfo>();
        managers.Add(new ManagerInfo
        {
            Address = Address.FromPublicKey("AAA".HexToByteArray()),
            ExtraData = "extra"
        });

        var guardians = new RepeatedField<Portkey.Contracts.CA.Guardian>();
        guardians.Add(new Portkey.Contracts.CA.Guardian
        {
            IsLoginGuardian = true,
            Salt = "salt",
            Type = GuardianType.OfEmail,
            IdentifierHash = HashHelper.ComputeFrom("G"),
            VerifierId = HashHelper.ComputeFrom("V")
        });

        var output = new GetHolderInfoOutput
        {
            CaHash = HashHelper.ComputeFrom("hash"),
            CaAddress = Address.FromPublicKey("CCC".HexToByteArray()),
            ManagerInfos = { managers },
            GuardianList = new GuardianList
            {
                Guardians = { guardians }
            }
        };

        var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        await grain.ValidateTransactionAsync("AELF", output, unsetList);
    }

    [Fact]
    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsyncTests()
    {
        var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        var input = await grain.GetSyncHolderInfoInputAsync("AELF", new TransactionInfo
        {
            TransactionId = HashHelper.ComputeFrom("txId").ToHex(),
            Transaction = new byte[] { 1, 2, 3 },
            BlockNumber = 1000
        });

        return input;
    }

    [Fact]
    public async Task SyncTransactionAsyncTests()
    {
        var input = await GetSyncHolderInfoInputAsyncTests();

        var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());

        await grain.SyncTransactionAsync("AELF", input);
    }
}
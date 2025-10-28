using AElf;
using AElf.Types;
using CAServer.Grains.Grain.ApplicationHandler;
using CAServer.Grains.Grain.RedPackage;
using CAServer.RedPackage;
using CAServer.RedPackage.Dtos;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Nethereum.Hex.HexConvertors.Extensions;
using Portkey.Contracts.CA;
using Portkey.Contracts.CryptoBox;
using Shouldly;
using Xunit;
using CAServer.Grains.State.ApplicationHandler;
using Xunit.Abstractions;

namespace CAServer.Grain.Tests.ContractService;

[Collection(ClusterCollection.Name)]
public class ContractServiceGrainTests : CAServerGrainTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ContractServiceGrainTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task CreateHolderInfoAsyncTests()
    {
        // var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());
        //
        // var result = await grain.CreateHolderInfoAsync(new CreateHolderDto
        // {
        //     ChainId = "AELF",
        //     GuardianInfo = new GuardianInfo
        //     {
        //         Type = GuardianType.OfEmail,
        //         IdentifierHash = HashHelper.ComputeFrom("G"),
        //         VerificationInfo = new VerificationInfo
        //         {
        //             Id = HashHelper.ComputeFrom("V"),
        //             Signature = ByteString.Empty,
        //             VerificationDoc = "doc"
        //         }
        //     },
        //     ManagerInfo = new ManagerInfo
        //     {
        //         Address = Address.FromPublicKey("AAA".HexToByteArray()),
        //         ExtraData = "extra"
        //     }
        // });
    }

    [Fact]
    public async Task SocialRecoveryAsyncTests()
    {
        // var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());
        //
        // await grain.SocialRecoveryAsync(new SocialRecoveryDto());
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

        // var output = new GetHolderInfoOutput
        // {
        //     CaHash = HashHelper.ComputeFrom("hash"),
        //     CaAddress = Address.FromPublicKey("CCC".HexToByteArray()),
        //     ManagerInfos = { managers },
        //     GuardianList = new GuardianList
        //     {
        //         Guardians = { guardians }
        //     }
        // };

        // var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());
        //
        // await grain.ValidateTransactionAsync("AELF", output, unsetList);
    }

    [Fact]
    public async Task<SyncHolderInfoInput> GetSyncHolderInfoInputAsyncTests()
    {
        // var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());
        //
        // var input = await grain.GetSyncHolderInfoInputAsync("AELF", new TransactionInfo
        // {
        //     TransactionId = HashHelper.ComputeFrom("txId").ToHex(),
        //     Transaction = new byte[] { 1, 2, 3 },
        //     BlockNumber = 1000
        // });

        return new SyncHolderInfoInput();
    }

    [Fact]
    public async Task SyncTransactionAsyncTests()
    {
        // var input = await GetSyncHolderInfoInputAsyncTests();
        //
        // var grain = Cluster.Client.GetGrain<IContractServiceGrain>(Guid.NewGuid());
        //
        // await grain.SyncTransactionAsync("AELF", input);
    }
    
    [Fact]
    public async void SendTransferCryptoBoxToChainAsyncTest()
    {
        var redPackageKeyGrain = Cluster.Client.GetGrain<IRedPackageKeyGrain>(Guid.Parse("6f720cbc-02ed-4467-92bc-76461d957745"));
        var res = await redPackageKeyGrain.GenerateKey();

        var list = new List<TransferCryptoBoxInput>()
        {
            new TransferCryptoBoxInput()
            {
                Amount = 1701075501959,
                Receiver = Address.FromBase58("2dni1t2hmZxtEE1tTiAWQ7Fm7hrc42wWvc1jyxAzDT6KGwHhDf"),
                CryptoBoxSignature = await redPackageKeyGrain.GenerateSignature("ELF--0--0.39")
            }
        };

        var sendInput = new TransferCryptoBoxesInput()
        {
            TransferCryptoBoxInputs = {list}
        };
    }
    
    private async Task GrabRedPackage_test()
    {
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var userId3 = Guid.NewGuid();
        var redPackageId = Guid.NewGuid();
        var redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(redPackageId);
        var input = NewSendRedPackageInputDto(redPackageId);
        input.Count = 2;
        await redPackageGrain.CreateRedPackage(input, 8, 1, userId1,86400000);
        var res = await redPackageGrain.GrabRedPackage(userId1, "xxxx");
        res.Success.ShouldBe(true);
        await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageFullyClaimed);
        
        redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1,86400000);
        await redPackageGrain.CancelRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageCancelled);
        await redPackageGrain.ExpireRedPackage();
        res = await redPackageGrain.GrabRedPackage(userId3, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageExpired);

        redPackageGrain = Cluster.Client.GetGrain<ICryptoBoxGrain>(Guid.NewGuid());
        await redPackageGrain.CreateRedPackage(NewSendRedPackageInputDto(Guid.NewGuid()), 8, 1, userId1,86400000);
        await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res = await redPackageGrain.GrabRedPackage(userId2, "xxxx");
        res.Success.ShouldBe(false);
        res.Data.ErrorMessage.ShouldBe(RedPackageConsts.RedPackageUserGrabbed);
    }
    
    private SendRedPackageInputDto NewSendRedPackageInputDto(Guid redPackageId)
    {
        return new SendRedPackageInputDto()
        {
            Id = new Guid("6f720cbc-02ed-4467-92bc-76461d957745"),
            Type = RedPackageType.Random,
            Count = 500,
            TotalAmount = "1000000",
            Memo = "this is my first memo",
            ChainId = "AELF",
            Symbol = "ELF",
            ChannelUuid = "eff010f5d9dd4df986a20251ae634e86",
            RawTransaction = "xxxxx",
            Message = "anyway"
        };
    }
}
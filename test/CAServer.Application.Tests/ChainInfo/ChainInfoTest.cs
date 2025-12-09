using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using CAServer.Chain;
using CAServer.Common;
using CAServer.Options;
using CAServer.Signature.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Portkey.Contracts.CA;
using Shouldly;
using Xunit;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;
using IChainAppService = CAServer.Chain.IChainAppService;
using Google.Protobuf;
using Xunit.Abstractions;

namespace CAServer.ChainInfo;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ChainInfoTest : CAServerApplicationTestBase
{
    private const string DefaultChainId = "AELF";
    private const string DefaultChainName = "DefaultChainName";
    private const string DefaultEndPoint = "DefaultEndPoint";
    private const string DefaultExplorerUrl = "DefaultExplorerUrl";
    private const string DefaultCaContractAddress = "DefaultCaContractAddress";

    private readonly IChainAppService _chainsService;
    private readonly IContractProvider _contractProvider;
    public readonly ITestOutputHelper _output;

    public ChainInfoTest(ITestOutputHelper output)
    {
        _chainsService = GetRequiredService<IChainAppService>();
        _contractProvider = GetRequiredService<IContractProvider>();
        _output = output;
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockChainOptions());
        services.AddSingleton(GetSignatureServerOptions());
    }

    [Fact]
    public async Task GetInfo()
    {
        var raw =
            "0a220a203000800ce18e6de0fc576a48759d9dc90a23f0ded388316b0f9f1274a45b809b12220a20f06d8236fbd12d260d783b9e7eac1d6f15880e594b65bdc25d131f9a66b2dc4318c683dc042204d946e09c2a0d476574486f6c646572496e666f32240a220a20024ab37e06c4342de4896e957df9b9be8feeb9ed95f83233176ab3a45c26289d82f1044161d2002b945b7e09ec2d2a460b1bb00157a6523f429d4a7babd01967eac4e9c4189e2cf601f7e6959d07d0fd5ba0b17c1f166d7081e34ede24a0c9a8788832a700";
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(raw));

        // var client = new AElfClient("http://192.168.66.106:8000");
        // await client.IsConnectedAsync();
        // var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        // {
        //     RawTransaction = raw
        // });

        GetHolderInfoOutput output = new GetHolderInfoOutput();
        output.MergeFrom(AElf.ByteArrayHelper.HexStringToByteArray(
            "0a220a201800bf4347c32b61b38e12b3d043d497a3652b0c6c048f45189ba04fc32e022112220a2057e957832e471b99fb6fb592f109899f31fdba5e919b9689512993e6e6881ec01ada010a6c12220a2051319dfb06df3edd7acee68b11ef7ed85ae905b9ae71e5f3b4597d4effac69b01a220a20a93874eecde8ebefaf6163cbc067c24982985c55dabb8ecb6a9bb020dde7f4f12220353238326366303535626231346338303838363238326663353465363136646428010a6a12220a20191bd54fbbe3fd064b80f31fa45fce366f525976e5cdb820e93d26bbb3e5f2551a220a20a93874eecde8ebefaf6163cbc067c24982985c55dabb8ecb6a9bb020dde7f4f12220353238326366303535626231346338303838363238326663353465363136646422aa010a220a2014ad6abf0cac5b916dbc6f9e8c0833b385646205736b88e910a5d7d180227f391283017b227472616e73616374696f6e54696d65223a313639343734393131333938372c22646576696365496e666f223a22736e4f6e654b73587a72596549697474624f79426842552f7536425579343541636248504b343137546a486c4b7668765653336978696e62472b63584a426468222c2276657273696f6e223a22322e302e30227d22aa010a220a20c14baf67f70fc95327bf632a22220cdf32a1725694055f0e2c901344a015ba5d1283017b227472616e73616374696f6e54696d65223a313639353032393236323434322c22646576696365496e666f223a224b4a34352f2b5236384e5251547a397a7945464431614756652b45366c6a35666c73722f67476c744d41676d364773665a436f6c544f6c4b6c30505775787257222c2276657273696f6e223a22322e302e30227d289bf4e104329501080312030000001a1b0805120202011a0f0a0d677561726469616e436f756e741a0208031a300807120202021a170a15677561726469616e417070726f766564436f756e741a0f0a0d677561726469616e436f756e741a3d0807120202001a170a15677561726469616e417070726f766564436f756e741a1c080a120202011a0f0a0d677561726469616e436f756e741a0308f02e"));
        var sss = "sss";
    }

    public string GetAddress(string publicKeyVal)
    {
        var publicKey = ByteArrayHelper.HexStringToByteArray(publicKeyVal);
        var address = Address.FromPublicKey(publicKey).ToBase58();
        return address;
    }

    //[Fact]
    public async Task GetHolder()
    {
        var hash = Hash.LoadFromHex("f060ddb5fa139cfe0174f26a51894f4a471b0cf74efce40f79e12d5b0aa07049");
        var mainChain = await _contractProvider.GetHolderInfoAsync(hash, null, "AELF");
        // var sideChain = await _contractProvider.GetHolderInfoAsync(hash, null, "tDVW");

        var managerAddress =
            GetAddress(
                "040e23c6a94cdac257808f46babd275c88819d1c34f34047878da8f6ec4df4cf350b0259cc83a1e5f8fd2da410dda0f391df69b0a51cbd7db418518213c1de0b59");
        _output.WriteLine(managerAddress);

        var managerInfo_mainChain = mainChain.ManagerInfos.FirstOrDefault(t => t.Address.ToBase58() == managerAddress);
        managerInfo_mainChain.ShouldNotBeNull();
    }

    private IOptions<ChainOptions> MockChainOptions()
    {
        var mockOptions = new Mock<IOptions<ChainOptions>>();
        mockOptions.Setup(o => o.Value).Returns(
            new ChainOptions
            {
                ChainInfos = new Dictionary<string, Grains.Grain.ApplicationHandler.ChainInfo>()
                {
                    ["AELF"] = new Grains.Grain.ApplicationHandler.ChainInfo()
                    {
                        ChainId = "AELF",
                        BaseUrl = "http://192.168.66.106:8000",
                        ContractAddress = "iupiTuL2cshxB9UNauXNXe9iyCcqka7jCotodcEHGpNXeLzqG",
                        PublicKey =
                            "0438ad713d76220ddfdac35e2b978f645cf254946d310b0e891201a7d8d36ef3341077d8a40b2fd79b1cfa91b3f3d675933d2ef761af9fa693cf2e36903404a32e",
                        IsMainChain = true
                    },
                    ["tDVW"] = new Grains.Grain.ApplicationHandler.ChainInfo()
                    {
                        ChainId = "tDVW",
                        BaseUrl = "http://192.168.66.106:8000",
                        ContractAddress = "2ptQUF1mm1cmF3v8uwB83iFCD46ynHLt4fxYoPNpCWRSBXwAEJ",
                        PublicKey =
                            "0438ad713d76220ddfdac35e2b978f645cf254946d310b0e891201a7d8d36ef3341077d8a40b2fd79b1cfa91b3f3d675933d2ef761af9fa693cf2e36903404a32e"
                    }
                }
            });
        return mockOptions.Object;
    }

    private IOptionsSnapshot<SignatureServerOptions> GetSignatureServerOptions()
    {
        var options = new Mock<IOptionsSnapshot<SignatureServerOptions>>();

        options.Setup(t => t.Value).Returns(new SignatureServerOptions()
        {
            BaseUrl = "http://192.168.66.240:18080/api/app/signature"
        });
        return options.Object;
    }

    [Fact]
    public async Task Create_Success_Test()
    {
        var result = await _chainsService.CreateAsync(new CreateUpdateChainDto
        {
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress
        });

        result.ShouldNotBeNull();
        result.ChainId.ShouldBe(DefaultChainId);
    }

    [Fact]
    public async Task Update_Success_Test()
    {
        var chainInfo = new CreateUpdateChainDto
        {
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress
        };

        await _chainsService.CreateAsync(chainInfo);

        var newChainName = "ChangedName";
        chainInfo.ChainName = newChainName;

        var result = await _chainsService.UpdateAsync(chainInfo.ChainId, chainInfo);

        result.ShouldNotBeNull();
        result.ChainId.ShouldBe(DefaultChainId);
    }

    [Fact]
    public async Task Update_ChainId_Not_Match_Test()
    {
        try
        {
            var chainInfo = new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = DefaultCaContractAddress
            };

            await _chainsService.CreateAsync(chainInfo);

            var newChainName = "ChangedName";
            chainInfo.ChainName = newChainName;

            var result = await _chainsService.UpdateAsync("newChainId", chainInfo);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("chainId can not modify.");
        }
    }

    [Fact]
    public async Task Update_Not_Exist_Test()
    {
        try
        {
            var chainInfo = new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = DefaultCaContractAddress
            };

            await _chainsService.UpdateAsync(DefaultChainId, chainInfo);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Chain not exist.");
        }
    }

    [Fact]
    public async Task Delete_Success_Test()
    {
        var chainInfo = new CreateUpdateChainDto
        {
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress
        };

        await _chainsService.CreateAsync(chainInfo);

        await _chainsService.DeleteAsync(chainInfo.ChainId);
    }

    [Fact]
    public async Task Delete_Not_Exist_Test()
    {
        try
        {
            await _chainsService.DeleteAsync("chainInfo.ChainId");
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Chain not exist.");
        }
    }
}
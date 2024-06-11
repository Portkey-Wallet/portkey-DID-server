using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Portkey.Contracts.CryptoBox;
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
        services.AddSingleton(GetContractOption());
        services.AddSingleton(GetSignatureServerOptions());
    }

    [Fact]
    public async Task GetInfo()
    {
        var raw =
            "0a220a2025de51e236960213ffe916183a89f32b30b99be808c5dbc6a8fcab4d2eb0812312220a20c20db44e9239a8e374da2d75cf508f60c9249f5279d5a5ecc38b82e1300a5ff518c7c2ca3a2204130ff0092a0f526566756e6443727970746f426f7832b0010a2433363638353639362d646533372d346164342d383435382d3636646434623132623332381080ade2041a82013036353937316261613939346239313064343464323561633837353633356336366561613635306266396461643438393233393939303162656166633738386633306439336133343864343934333366663336326463656634356366333832333636643463363939373264313931633436303431396233363730373730343333303182f1044139f93d31e7b44a6110a60ba4ca34422f29d0c5876392faaab8aa97c1f4cce56804cc06fb139dfa94efe312456d4d83ab3564663e64e70afce2f1cdb1bace439301";
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(raw));

        var param = RefundCryptoBoxInput.Parser.ParseFrom(transaction.Params);
        var client = new AElfClient("https://tdvw-test-node.aelf.io");
        await client.IsConnectedAsync();
        var result = await client.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = raw
        });

        var res = await client.GetTransactionResultAsync("5eeee4e65abaf0975d0189c225f655e7995de52796def8ecb80db8c60515197f");

        var add = GetAddress(
            "048fee2f5fd656c2c5bd13f86b691747ef3b1960347e7d0f0569873b104afef95d45de22586fe4a3639a0b78cc61a1e29f62c8327e108ead00f42c7f692ce5bdfc");

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

    private IOptionsSnapshot<ContractOptions> GetContractOption()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<ContractOptions>>();
        mockOptionsSnapshot.Setup(t => t.Value).Returns(new ContractOptions()
        {
            CommonPrivateKeyForCallTx = "aee9944b684505b51c2eefc54b6735453160a74f27c158df65d2783fafa81e57"
        });

        return mockOptionsSnapshot.Object;
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
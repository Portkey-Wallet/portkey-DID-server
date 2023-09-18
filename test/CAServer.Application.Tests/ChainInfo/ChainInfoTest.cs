using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using CAServer.Chain;
using CAServer.Common;
using CAServer.Options;
using CAServer.Signature;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;
using ChainOptions = CAServer.Grains.Grain.ApplicationHandler.ChainOptions;

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

    public ChainInfoTest()
    {
        _chainsService = GetRequiredService<IChainAppService>();
        _contractProvider = GetRequiredService<IContractProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(MockSignatureServerOptions());
        services.AddSingleton(GetContractOption());
        services.AddSingleton(GetSignatureServerOptions());
    }

    [Fact]
    public async Task GetHolder()
    {
        var hash = Hash.LoadFromHex("a0a96f0c4b45719091ede2634dc05b277df4c68c39e2a3465c0c38f61a7b67fa");
        var mainChain = await _contractProvider.GetHolderInfoAsync(hash, null, "AELF");
        var sideChain = await _contractProvider.GetHolderInfoAsync(hash, null, "tDVW");

        var m_root = mainChain.GuardianList;
        var s_root = sideChain.GuardianList;

        var sss = "sss";
    }

    private IOptions<ChainOptions> MockSignatureServerOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptions<ChainOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new ChainOptions
            {
                ChainInfos = new Dictionary<string, Grains.Grain.ApplicationHandler.ChainInfo>()
                {
                    ["AELF"] = new Grains.Grain.ApplicationHandler.ChainInfo()
                    {
                        ChainId = "AELF",
                        BaseUrl = "http://192.168.67.18:8000",
                        ContractAddress = "2u6Dd139bHvZJdZ835XnNKL5y6cxqzV9PEWD5fZdQXdFZLgevc",
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
        return mockOptionsSnapshot.Object;
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
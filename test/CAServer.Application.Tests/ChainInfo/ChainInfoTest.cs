using System;
using System.Threading.Tasks;
using CAServer.Chain;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

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

    public ChainInfoTest()
    {
        _chainsService = GetRequiredService<IChainAppService>();
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
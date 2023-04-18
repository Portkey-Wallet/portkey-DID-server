using System;
using System.Threading.Tasks;
using CAServer.Chain;
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
        _chainsService = GetService<IChainAppService>();
    }

    [Fact]
    public async Task CreateOrUpdate_Success_Test()
    {
        await _chainsService.CreateAsync(new CreateUpdateChainDto
        {
            ChainId = DefaultChainId,
            ChainName = DefaultChainName,
            EndPoint = DefaultEndPoint,
            ExplorerUrl = DefaultExplorerUrl,
            CaContractAddress = DefaultCaContractAddress
        });
    }

    [Fact]
    public async Task CreateOrUpdate_Body_Empty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
            });
        }
        catch (Exception e)
        {
            Assert.True(e is not null);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_ChainId_IsNllOrEmpty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
                ChainId = "",
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = DefaultCaContractAddress
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_ChainName_IsNllOrEmpty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = "",
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = DefaultCaContractAddress
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_EndPoint_IsNllOrEmpty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = DefaultCaContractAddress
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_ExplorerUrl_IsNllOrEmpty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = "",
                CaContractAddress = DefaultCaContractAddress
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_CaContractAddress_IsNllOrEmpty_Test()
    {
        try
        {
            await _chainsService.CreateAsync(new CreateUpdateChainDto
            {
                ChainId = DefaultChainId,
                ChainName = DefaultChainName,
                EndPoint = DefaultEndPoint,
                ExplorerUrl = DefaultExplorerUrl,
                CaContractAddress = ""
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }
}
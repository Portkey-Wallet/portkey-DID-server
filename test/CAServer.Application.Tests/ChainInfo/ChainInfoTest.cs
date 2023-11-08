using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.Vote;
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
    public void Test_Time()
    {
        var date = GetDateTimeSeconds(1694448000);
        date.ToShortDateString().ShouldBe("2023/09/12");
    }

    public static DateTime GetDateTimeSeconds(long timestamp)
    {
        var begtime = timestamp * 10000000;
        var dt_1970 = new DateTime(1970, 1, 1, 8, 0, 0);
        var tricks_1970 = dt_1970.Ticks;
        var time_tricks = tricks_1970 + begtime;
        var dt = new DateTime(time_tricks);
        return dt;
    }

    [Fact]
    public async Task Create_Success_Test()
    {
        var str =
            "CiIKIPujE6mnDNgPZepaNKpE5Vx94qPfbsdjn4Tq6jUicil+EiIKICwkAc+P9QlkfXy9kO2NKc7OQ8VbCH6h/WyzjYnBKFLxGJUBIIC4wuuCCSoLCPL9uqkGEODe7lsyggEwNGE2NjA3YjcyZmEyNmY4NjQ0ZThlMjRhMTZmNjFjN2NiNTZjOWM4NTBkM2JhYTU2MGJmNGY4MzdjMmQ5OTM1MTFmYTY5OTZkNjBkNmJkYmQ0ODhkOWI3NDM4MTQ4NjBhOTM4MTYyN2E0Njk3N2VhZGY3Y2MzMjM2ZGMyMTZjOGZjOiIKIDnXHrX/Yd16Yc531Bs6TwrgcvgzYKZ1DjUXi66tKaap";

        var aaa = Org.BouncyCastle.Utilities.Encoders.Base64.Decode(str);
        var ttt = Voted.Parser.ParseFrom(aaa);
        var ccc = ttt.Amount;
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
using System;
using System.Threading.Tasks;
using CAServer.ClaimToken.Dtos;
using Shouldly;
using Xunit;

namespace CAServer.Common;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ContractProviderTest : CAServerApplicationTestBase
{
    private readonly IContractProvider _contractProvider;

    public ContractProviderTest()
    {
        _contractProvider = GetRequiredService<IContractProvider>();
    }

    [Fact]
    public async Task GetVerifierServersListAsyncTest()
    {
        try
        {
            var outPutNull = await _contractProvider.GetVerifierServersListAsync("TEST");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetVerifierServersListAsync_ChainId_NotFount_Test()
    {
        var result = await _contractProvider.GetVerifierServersListAsync("TEST1");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetBalanceAsyncTest()
    {
        try
        {
            var outPutNull = await _contractProvider.GetBalanceAsync("TEST", "address", "CPU");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetBalanceAsync_ChainId_NotFount_Test()
    {
        var result = await _contractProvider.GetBalanceAsync("TEST1", "address", "CPU");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task TransferAsyncTest()
    {
        try
        {
            var outPutNull = await _contractProvider.TransferAsync("TEST", new ClaimTokenRequestDto()
            {
                Amount = "100"
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task TransferAsync_ChainId_NotFount_Test()
    {
        var result = await _contractProvider.TransferAsync("TEST1", new ClaimTokenRequestDto());
        result.TransactionId.ShouldBeNull();
    }

    [Fact]
    public async Task ClaimTokenAsync()
    {
        try
        {
            await _contractProvider.ClaimTokenAsync("TEST", "CPU");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ClaimTokenAsync_ChainId_NotFount_Test()
    {
        await _contractProvider.ClaimTokenAsync("TEST1", "CPU");
    }
}
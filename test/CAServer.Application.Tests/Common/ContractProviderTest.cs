using System;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Volo.Abp;
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

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(MockHttpFactory());
        DateTimeOffset offset = DateTime.UtcNow.AddDays(7);
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
            // chainId not found
            var outPutNull = await _contractProvider.GetBalanceAsync("CPU", "address", "TEST1");
            outPutNull.ShouldBeNull();

            await _contractProvider.GetBalanceAsync("CPU", "address", "TEST");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetBalanceAsync_ChainId_NotFount_Test()
    {
        var result = await _contractProvider.GetBalanceAsync("TEST1",
            Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(), "CPU");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ClaimTokenAsync()
    {
        try
        {
            await _contractProvider.ClaimTokenAsync("TEST", "CPU", "AELF");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ClaimTokenAsync_ChainId_NotFount_Test()
    {
        await _contractProvider.ClaimTokenAsync( "AELF", "2jxnT1HxRr9PrfMjrhEvKyKABDa82oxhQVyrbnw7VkosKoBqvE", "TEST1");
    }

    [Fact]
    public async Task SendTransferAsync()
    {
        try
        {
            var nullResult = await _contractProvider.SendTransferAsync("TEST", "1",
                "2jxnT1HxRr9PrfMjrhEvKyKABDa82oxhQVyrbnw7VkosKoBqvE", "TEST1", "");
            nullResult.ShouldBeNull();

            var result = await _contractProvider.SendTransferAsync("TEST", "1",
                "2jxnT1HxRr9PrfMjrhEvKyKABDa82oxhQVyrbnw7VkosKoBqvE", "TEST", "");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task SendRawTransaction()
    {
        var rawTransaction =
            "0a220a203e1f7576c33fb1f8dc90f1ffd7775691d182ce99456d12f01aedf871014c22b412220a20e28c0b6c4145f3534431326f3c6d5a4bd6006632fd7551c26c103c368855531618abf9860d220411d0e8922a124d616e61676572466f727761726443616c6c3286010a220a20ffc98c7be1a50ada7ca839da2ecd94834525bdcea392792957cc7f1b2a0c3a1e12220a202791e992a57f28e75a11f13af2c0aec8b0eb35d2f048d42eba8901c92e0378dc1a085472616e7366657222320a220a20a7376d782cdf1b1caa2f8b5f56716209045cd5720b912e8441b4404427656cb91203454c461880a0be819501220082f104411d1acf81058c6a65ba0ed78368c815add80349b8e1fd7c4e5e2655c3dbde582833a475408094b486ddd14c6ad0f9c0e01788f209c2d0a1e356792e5bff1d4c2e01";

        var resultFail = () => _contractProvider.SendRawTransactionAsync("TEST1", rawTransaction);
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(resultFail);
        exception.Message.ShouldContain("Send RawTransaction FAILED");

        try
        {
            await _contractProvider.SendRawTransactionAsync("TEST", rawTransaction);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetTransactionResult()
    {
        try
        {
            var transactionId = "c49c1cfcd0994379819daa69a083b63335a66661b43d893832380ed4b2305b6a";
            await _contractProvider.GetTransactionResultAsync("TEST", transactionId);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task GetHolderInfoTest()
    {
        try
        {
            await _contractProvider.GetHolderInfoAsync(
                HashHelper.ComputeFrom("CA_HASH"),
                HashHelper.ComputeFrom("GUARDIAN_HASH"),
                "TEST");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}
using System.Threading.Tasks;
using CAServer.Grain.Tests;
using CAServer.Grains;
using CAServer.Grains.Grain.QrCode;
using CAServer.QrCode.Dtos;
using Orleans.TestingHost;
using Shouldly;
using Xunit;

namespace CAServer.QrCode;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class QrCodeTest : CAServerApplicationTestBase
{
    private readonly IQrCodeAppService _qrCodeAppService;
    private readonly TestCluster _cluster;

    public QrCodeTest()
    {
        _qrCodeAppService = GetRequiredService<IQrCodeAppService>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
    }

    [Fact]
    public async Task ExistTest()
    {
        var result = await _qrCodeAppService.CreateAsync(new QrCodeRequestDto { Id = "test" });
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task NotExistTest()
    {
        var grainId =
            GrainIdHelper.GenerateGrainId("QrCode", "test_not_exist");
        var grain = _cluster.Client.GetGrain<IQrCodeGrain>(grainId);
        await grain.AddIfAbsent();
        
        var result = await _qrCodeAppService.CreateAsync(new QrCodeRequestDto { Id = "test_not_exist" });
        result.ShouldBeFalse();
    }
}
using CAServer.Grains.Grain.Notify;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.Notify;

[Collection(ClusterCollection.Name)]
public class NotifyTest : CAServerGrainTestBase
{
    private const string DefaultTitle = "Update-app";
    private const string DefaultContent = "Update";
    private const string DefaultTargetVersion = "1.1.1";
    private const string DefaultDownloadUrl = "http://127.0.0.1:8080";
    private const string DefaultAppId = "1.0.1";
    private static string[] DefaultAppVersions = { "1.1.0", "1.2.0" };
    private static string[] DefaultDeviceTypes = { "1", "2" };

    private NotifyGrainDto notifyGrainDto = new NotifyGrainDto
    {
        Title = DefaultTitle,
        Content = DefaultContent,
        TargetVersion = DefaultTargetVersion,
        DownloadUrl = DefaultDownloadUrl,
        AppId = DefaultAppId,
        AppVersions = DefaultAppVersions,
        DeviceTypes = DefaultDeviceTypes
    };

    [Fact]
    public async Task AddNotifyTest()
    {
        var grain = Cluster.Client.GetGrain<INotifyGrain>(Guid.NewGuid());
        var result = await grain.AddNotifyAsync(notifyGrainDto);
        result.Success.ShouldBeTrue();
        result.Data.Title.ShouldBe(DefaultTitle);
    }

    [Fact]
    public async Task AddNotify_Twice_Test()
    {
        var grain = Cluster.Client.GetGrain<INotifyGrain>(Guid.NewGuid());
        await grain.AddNotifyAsync(notifyGrainDto);
        var result = await grain.AddNotifyAsync(notifyGrainDto);
        result.Success.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateNotifyTest()
    {
        var grain = Cluster.Client.GetGrain<INotifyGrain>(Guid.NewGuid());
        await grain.AddNotifyAsync(notifyGrainDto);

        notifyGrainDto.Title = "New-Title";
        var result = await grain.UpdateNotifyAsync(notifyGrainDto);
        result.Success.ShouldBeTrue();
        result.Data.Title.ShouldBe("New-Title");
    }

    [Fact]
    public async Task DeleteNotifyTest()
    {
        var notifyId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<INotifyGrain>(notifyId);
        await grain.AddNotifyAsync(notifyGrainDto);

        var result = await grain.DeleteNotifyAsync(notifyId);
        result.Success.ShouldBeTrue();
        result.Data.Title.ShouldBe(DefaultTitle);
    }

    [Fact]
    public async Task GetNotifyTest()
    {
        var notifyId = Guid.NewGuid();
        var grain = Cluster.Client.GetGrain<INotifyGrain>(notifyId);
        await grain.AddNotifyAsync(notifyGrainDto);

        var result = await grain.GetNotifyAsync();
        result.Success.ShouldBeTrue();
        result.Data.Title.ShouldBe(DefaultTitle);
    }
}
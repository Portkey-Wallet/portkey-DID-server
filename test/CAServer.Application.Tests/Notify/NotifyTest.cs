using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Notify;
using CAServer.Notify.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace CAServer.Notify;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class NotifyTest : CAServerApplicationTestBase
{
    private readonly INotifyAppService _notifyAppService;
    private readonly INESTRepository<NotifyRulesIndex, Guid> _notifyRulesRepository;

    public NotifyTest()
    {
        _notifyAppService = GetRequiredService<INotifyAppService>();
        _notifyRulesRepository = GetRequiredService<INESTRepository<NotifyRulesIndex, Guid>>();
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(GetIpInfo());
    }

    [Fact]
    public async Task Create_Test()
    {
        var dto = new CreateNotifyDto
        {
            Title = "Update Portkey",
            Content = "Update Portkey",
            AppId = "100001",
            TargetVersion = "1.2.2",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { DeviceType.Android },
            SendTypes = new[] { NotifySendType.None },
            ReleaseTime = DateTime.UtcNow,
            DownloadUrl = "http://127.0.0.1",
            IsForceUpdate = true,
            IsApproved = true
        };

        var createResult = await _notifyAppService.CreateAsync(dto);
        createResult.ShouldNotBeNull();
        createResult.AppId.ShouldBe(dto.AppId);

        var newTitle = "Update Portkey New";
        var updateDto = new UpdateNotifyDto
        {
            Title = newTitle,
            Content = "Update Portkey",
            AppId = "100001",
            TargetVersion = "1.2.2",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { DeviceType.Android },
            SendTypes = new[] { NotifySendType.None },
            ReleaseTime = DateTime.UtcNow,
            DownloadUrl = "http://127.0.0.1",
            IsForceUpdate = true,
            IsApproved = true
        };
        var updateResult = await _notifyAppService.UpdateAsync(createResult.Id, updateDto);

        updateResult.ShouldNotBeNull();
        updateResult.Title.ShouldBe(newTitle);
    }

    [Fact]
    public async Task Delete_Test()
    {
        var dto = new CreateNotifyDto
        {
            Title = "Update Portkey",
            Content = "Update Portkey",
            AppId = "100001",
            TargetVersion = "1.2.2",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { DeviceType.Android },
            SendTypes = new[] { NotifySendType.None },
            ReleaseTime = DateTime.UtcNow,
            DownloadUrl = "http://127.0.0.1",
            IsForceUpdate = true,
            IsApproved = true
        };

        var createDto = await _notifyAppService.CreateAsync(dto);
        await _notifyAppService.DeleteAsync(createDto.Id);
    }

    [Fact]
    public async Task Pull_Test()
    {
        var dto = new CreateNotifyDto
        {
            Title = "Update Portkey",
            Content = "Update Portkey",
            AppId = "100001",
            TargetVersion = "1.2.2",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { DeviceType.Android },
            SendTypes = new[] { NotifySendType.None },
            ReleaseTime = DateTime.UtcNow,
            DownloadUrl = "http://127.0.0.1",
            IsForceUpdate = true,
            IsApproved = true,
            NotifyId = 1
        };

        var createDto = await _notifyAppService.CreateAsync(dto);

        await _notifyRulesRepository.AddOrUpdateAsync(new NotifyRulesIndex
        {
            AppId = "100001",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { "Android" },
            SendTypes = new[] { NotifySendType.None },
            IsApproved = true,
            Id = createDto.Id,
            NotifyId = 1
        });

        var result = await _notifyAppService.PullNotifyAsync(new PullNotifyDto()
        {
            DeviceId = "qwer",
            AppId = "100001",
            DeviceType = DeviceType.Android,
            AppVersion = "1.0.0"
        });

        result.Title.ShouldBe("Update Portkey");
    }
}
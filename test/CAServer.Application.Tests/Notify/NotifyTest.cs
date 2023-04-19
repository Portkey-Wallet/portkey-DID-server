using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Grains.Grain.Notify;
using CAServer.Notify.Dtos;
using Shouldly;
using Xunit;

namespace CAServer.Notify;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class NotifyTest : CAServerApplicationTestBase
{
    private readonly INotifyAppService _notifyAppService;
    private readonly INESTRepository<NotifyRulesIndex, Guid> _notifyRulesRepository;

    public NotifyTest()
    {
        _notifyAppService = GetService<INotifyAppService>();
        _notifyRulesRepository = GetService<INESTRepository<NotifyRulesIndex, Guid>>();
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

        var result = await _notifyAppService.CreateAsync(dto);
        result.ShouldNotBeNull();
        result.AppId.ShouldBe(dto.AppId);
    }

    [Fact]
    public async Task Update_Test()
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

        var createDto = await _notifyAppService.CreateAsync(dto);
        var result = await _notifyAppService.UpdateAsync(createDto.Id, updateDto);

        result.ShouldNotBeNull();
        result.Title.ShouldBe(newTitle);
    }

    [Fact]
    public async Task Update_Not_Exist_Test()
    {
        try
        {
            var updateDto = new UpdateNotifyDto
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
            await _notifyAppService.UpdateAsync(Guid.Empty, updateDto);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(NotifyMessage.NotExistMessage);
        }
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
    public async Task Delete_Not_Exist_Test()
    {
        try
        {
            await _notifyAppService.DeleteAsync(Guid.NewGuid());
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(NotifyMessage.NotExistMessage);
        }
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
            IsApproved = true
        };

        var createDto = await _notifyAppService.CreateAsync(dto);
        
        await _notifyRulesRepository.AddOrUpdateAsync(new NotifyRulesIndex
        {
            AppId = "100001",
            AppVersions = new[] { "1.0.0", "1.1.0" },
            DeviceTypes = new[] { "Android" },
            SendTypes = new[] { NotifySendType.None },
            IsApproved = true,
            Id = createDto.Id
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
    
    [Fact]
    public async Task Pull_Not_Exist_Test()
    {
        try
        {
            await _notifyRulesRepository.AddOrUpdateAsync(new NotifyRulesIndex
            {
                AppId = "100001",
                AppVersions = new[] { "1.0.0", "1.1.0" },
                DeviceTypes = new[] { "Android" },
                SendTypes = new[] { NotifySendType.None },
                IsApproved = true,
                Id = Guid.NewGuid()
            });

            var result = await _notifyAppService.PullNotifyAsync(new PullNotifyDto()
            {
                DeviceId = "qwer",
                AppId = "100001",
                DeviceType = DeviceType.Android,
                AppVersion = "1.0.0"
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(NotifyMessage.NotExistMessage);
        }
    }
    
}
using CAServer.Entities.Es;
using CAServer.Notify.Etos;
using CAServer.Search;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.Notity;

public class NotifyHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public NotifyHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_NewNotity()
    {
        var notity = new NotifyEto
        {
            Id = Guid.NewGuid()
        };
        await _eventBus.PublishAsync(notity);

        var result = await _searchAppService.GetListByLucenceAsync("notifyrulesindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var info = JsonConvert.DeserializeObject<PagedResultDto<NotifyRulesIndex>>(result);
        info.ShouldNotBeNull();
        info.TotalCount.ShouldNotBe(0);
    }
    
    [Fact]
    public async Task HandlerEvent_DeleteNotify()
    {
        
        var addnotity = new NotifyEto
        {
            Id = Guid.Parse("3cbc7529-4f3b-478b-b79d-6a34dc07fc6e")
        };
        await _eventBus.PublishAsync(addnotity);

        var addresult = await _searchAppService.GetListByLucenceAsync("notifyrulesindex", new GetListInput()
        {
            MaxResultCount = 1
        });
        addresult.ShouldNotBeNull();
        
        var notity = new DeleteNotifyEto
        {
            Id = Guid.Parse("3cbc7529-4f3b-478b-b79d-6a34dc07fc6e")
        };
        await _eventBus.PublishAsync(notity);

        var result = await _searchAppService.GetListByLucenceAsync("notifyrulesindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var info = JsonConvert.DeserializeObject<PagedResultDto<NotifyRulesIndex>>(result);
        info.ShouldNotBeNull();
        info.Items.Count.ShouldBe(0);
    }
    
}
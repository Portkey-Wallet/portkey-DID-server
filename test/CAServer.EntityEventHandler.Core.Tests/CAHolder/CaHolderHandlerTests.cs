using AElf;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Search;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAHolder;

public class   CaHolderHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public CaHolderHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_NewNotity()
    {
        var user = new CreateUserEto
        {
            CaHash = HashHelper.ComputeFrom("a23322344aa").ToString(),
            Id = Guid.NewGuid(),
            Nickname = "test333",
            UserId = Guid.NewGuid()
        };
        // await _eventBus.PublishAsync(user);
        //
        // var result = await _searchAppService.GetListByLucenceAsync("caholderindex", new GetListInput()
        // {
        //     MaxResultCount = 1
        // });
        //
        // result.ShouldNotBeNull();
        // var extra = JsonConvert.DeserializeObject<PagedResultDto<CAHolderIndex>>(result);
        // extra.ShouldNotBeNull();
        // extra.Items[0].NickName.ShouldBe("test333");
    }
}
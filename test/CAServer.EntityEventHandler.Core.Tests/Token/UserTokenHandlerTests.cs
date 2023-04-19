using AElf;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Search;
using CAServer.Tokens;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.Token;

public class UserTokenHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly IDistributedEventHandler<CreateUserEto> _userEventHandler;
    private readonly ISearchAppService _searchAppService;
    private readonly IUserTokenAppService _userTokenAppService;

    public UserTokenHandlerTests()
    {
        _userEventHandler = GetRequiredService<MockUserHandler>();
        _searchAppService = GetRequiredService<ISearchAppService>();
        _userTokenAppService = GetRequiredService<IUserTokenAppService>();
    }

    [Fact]
    public async Task HandlerEvent_NewUserToken()
    {
        var user = new CreateUserEto
        {
            CaHash = HashHelper.ComputeFrom("aaa").ToString(),
            Id = Guid.NewGuid(),
            Nickname = "test",
            UserId = Guid.NewGuid()
        };
        await _userEventHandler.HandleEventAsync(user);

        Thread.Sleep(2000);
        
        var result = await _searchAppService.GetListByLucenceAsync("usertokenindex", new GetListInput());
        result.ShouldNotBeNull();
        var info = JsonConvert.DeserializeObject<PagedResultDto<UserTokenIndex>>(result);

        var userTokenId = info.Items[0].Id;
        userTokenId.ShouldNotBe(Guid.Empty);
    }
}
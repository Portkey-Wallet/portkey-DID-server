using CAServer.Entities.Es;
using CAServer.Search;
using CAServer.Verifier.Etos;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;
using Newtonsoft.Json;


namespace CAServer.EntityEventHandler.Tests.ExtraInfo;

public class UserExtraInfoHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public UserExtraInfoHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }


    [Fact]
    public async Task HandlerEvent_NewExtrainfo()
    {
        var extraInfo = new UserExtraInfoEto
        {
            Id = Guid.NewGuid().ToString(),
            FullName = "test",
            FirstName ="test" ,
            LastName ="test",
            Email ="test@qq.com",
            Picture =  "http://2323.com/pic.jpg", 
            VerifiedEmail = true,
            IsPrivateEmail = true,
            GuardianType   ="test",
            AuthTime   = new DateTime()
        };
        await _eventBus.PublishAsync(extraInfo);

        var result = await _searchAppService.GetListByLucenceAsync("userextrainfoindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<UserExtraInfoIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].Email.ShouldBe("test@qq.com");
        
       
       
    }
}
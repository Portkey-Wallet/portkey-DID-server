using CAServer.Entities.Es;
using CAServer.Search;
using CAServer.Verifier.Etos;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAVerifier;

public class    CaVerifierHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public CaVerifierHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_NewNotity()
    {
        var verifer = new VerifierCodeEto
        {
            VerifierSessionId = Guid.NewGuid(),
            Type ="1" ,
            GuardianAccount  ="test00001" ,
            VerifierId  ="test0000322342" ,
            ChainId  ="test0032351" ,
        };
        // await _eventBus.PublishAsync(verifer);
        //
        // var result = await _searchAppService.GetListByLucenceAsync("guardianindex", new GetListInput()
        // {
        //     MaxResultCount = 1
        // });
        //
        // result.ShouldNotBeNull();
        // var info = JsonConvert.DeserializeObject<PagedResultDto<GuardianIndex>>(result);
        // info.ShouldNotBeNull();
        // info.Items[0].Identifier.ShouldBe("test");

    }
}
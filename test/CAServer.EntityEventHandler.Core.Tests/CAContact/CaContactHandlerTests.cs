using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Search;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAContact;

public class CaContactHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public CaContactHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_ContactAddressEto()
    {
        ContactAddressEto add01 = new ContactAddressEto();
        add01.Address = "013232323232332";
        add01.ChainId = "chain01";

        ContactAddressEto add02 = new ContactAddressEto();
        add02.Address = "023dsdsfds2";
        add02.ChainId = "chain02";
        List<ContactAddressEto> list = new List<ContactAddressEto>();
        list.Add(add01);
        list.Add(add02);
        var chain = new ContactCreateEto
        {
            Id = Guid.NewGuid(),
            Index = "1",
            Name = "test",
            Addresses = list,
            UserId = Guid.NewGuid(),
            IsDeleted = true
        };
        await _eventBus.PublishAsync(chain);

        var result = await _searchAppService.GetListByLucenceAsync("contactindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<ContactIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].Name.ShouldBe("test");
    }

    [Fact]
    public async Task HandlerEvent_ContactUpdateEto()
    {
        var createCaHolder = new CreateCAHolderEto
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CaAddress = string.Empty,
            CaHash = string.Empty,
            Nickname = string.Empty,
            CreateTime = DateTime.UtcNow
        };
        ContactAddressEto add01 = new ContactAddressEto();
        add01.Address = "013232323232332";
        add01.ChainId = "chain01";

        ContactAddressEto add02 = new ContactAddressEto();
        add02.Address = "023dsdsfds2";
        add02.ChainId = "chain02";
        List<ContactAddressEto> list = new List<ContactAddressEto>();
        list.Add(add01);
        list.Add(add02);
        var addEto = new ContactCreateEto
        {
            Id = Guid.Parse("f77dd0c8-3af4-4d3b-a739-c80bbd79a322"),
            Index = "1",
            Name = "test",
            Addresses = list,
            UserId = Guid.Parse("fde64344-e2b3-485b-8c6d-07954cc76669"),
            IsDeleted = true
        };
        await _eventBus.PublishAsync(addEto);
        var addresult = await _searchAppService.GetListByLucenceAsync("contactindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        addresult.ShouldNotBeNull();

        var updateEto = new ContactUpdateEto
        {
            Id = Guid.Parse("f77dd0c8-3af4-4d3b-a739-c80bbd79a322"),
            Index = "2",
            Name = "test2",
            UserId = Guid.Parse("fde64344-e2b3-485b-8c6d-07954cc76669")
        };
        await _eventBus.PublishAsync(updateEto);

        var result = await _searchAppService.GetListByLucenceAsync("contactindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var extra = JsonConvert.DeserializeObject<PagedResultDto<ContactIndex>>(result);
        extra.ShouldNotBeNull();
        extra.Items[0].Name.ShouldBe("test2");
    }
}
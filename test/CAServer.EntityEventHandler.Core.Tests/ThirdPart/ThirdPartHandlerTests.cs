using CAServer.Entities.Es;
using CAServer.Search;
using CAServer.ThirdPart.Etos;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.ThirdPart;

public class ThirdPartHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public ThirdPartHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_NewThirdPart()
    {
        var order = new OrderEto
        {
            Id = Guid.NewGuid(),
            UserId =  Guid.NewGuid(),
            MerchantName ="test01",
            TransDirect ="test01",
            Address ="test01",
            Crypto ="test01",
            CryptoPrice ="test01",
            Fiat ="test01",
            FiatAmount ="test01",
            Status ="test01",
            LastModifyTime ="test01",
            IsDeleted = true,
            CryptoQuantity ="test01",
            PaymentMethod ="test01",
            TxTime ="test01",
            ReceivingMethod ="test01",
            ReceiptTime ="test01"
        };
        await _eventBus.PublishAsync(order);

        var result = await _searchAppService.GetListByLucenceAsync("ramporderindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var chainInfo = JsonConvert.DeserializeObject<PagedResultDto<RampOrderIndex>>(result);
        chainInfo.ShouldNotBeNull();
        chainInfo.Items[0].Status.ShouldBe("test01");
    }
}
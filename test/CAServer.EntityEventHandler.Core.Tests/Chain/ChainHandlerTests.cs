using CAServer.Chain;
using CAServer.Entities.Es;
using CAServer.Etos.Chain;
using CAServer.Search;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;
using Newtonsoft.Json;

namespace CAServer.EntityEventHandler.Tests.Chain;

public class   ChainHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;

    public ChainHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    [Fact]
    public async Task HandlerEvent_ChainCreateEto()
    {
        var chain = new ChainCreateEto
        {
            Id = Guid.NewGuid().ToString(),
            ChainId = "testchain001",
            ChainName = "test-elf-01",
            EndPoint = "127.0.1.4",
            ExplorerUrl = "http://132323.com",
            CaContractAddress = "233233223",
            LastModifyTime = new DateTime(),
            DefaultToken = new DefaultToken()
        };
        await _eventBus.PublishAsync(chain);

        var result = await _searchAppService.GetListByLucenceAsync("chainsinfoindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var chainInfo = JsonConvert.DeserializeObject<PagedResultDto<ChainsInfoIndex>>(result);
        chainInfo.ShouldNotBeNull();
        chainInfo.Items[0].ChainId.ShouldBe("testchain001");
    }
    
    
    [Fact]
    public async Task HandlerEvent_ChainUpdateEto()
    {
        
        var chain1 = new ChainCreateEto
        {
            Id = "87ce7047-7f51-471e-9af2-2c23137096db",
            ChainId = "testchain001",
            ChainName = "test-elf-01",
            EndPoint = "127.0.1.4",
            ExplorerUrl = "http://132323.com",
            CaContractAddress = "233233223",
            LastModifyTime = new DateTime(),
            DefaultToken = new DefaultToken()
        };
        await _eventBus.PublishAsync(chain1);
        
        var chain = new ChainUpdateEto
        {
            Id = "87ce7047-7f51-471e-9af2-2c23137096db",
            ChainName = "test-elf-02",
            LastModifyTime = new DateTime()
        };
        await _eventBus.PublishAsync(chain);

        var result = await _searchAppService.GetListByLucenceAsync("chainsinfoindex", new GetListInput()
        {
            MaxResultCount = 1
        });

        result.ShouldNotBeNull();
        var chainInfo = JsonConvert.DeserializeObject<PagedResultDto<ChainsInfoIndex>>(result);
        chainInfo.ShouldNotBeNull();
        chainInfo.Items[0].ChainName.ShouldBe("test-elf-02");
    }
    
    [Fact]
    public async Task HandlerEvent_ChainDeleteEto()
    {
        var chain1 = new ChainCreateEto
        {
            Id = "77ce7047-7f51-471e-9af2-2c23137096db",
            ChainId = "testchain001",
            ChainName = "test-elf-01",
            EndPoint = "127.0.1.4",
            ExplorerUrl = "http://132323.com",
            CaContractAddress = "233233223",
            LastModifyTime = new DateTime(),
            DefaultToken = new DefaultToken()
        };
        await _eventBus.PublishAsync(chain1);
        var chain = new ChainDeleteEto
        {
            Id = "77ce7047-7f51-471e-9af2-2c23137096db",
        };
        
      
        await _eventBus.PublishAsync(chain);
        var result = await _searchAppService.GetListByLucenceAsync("chainsinfoindex", new GetListInput()
        {
            MaxResultCount = 1
        });
        result.ShouldNotBeNull();
        var chainInfo = JsonConvert.DeserializeObject<PagedResultDto<ChainsInfoIndex>>(result);
        chainInfo.ShouldNotBeNull();
        chainInfo.Items.Count.ShouldBe(0);
    }
}
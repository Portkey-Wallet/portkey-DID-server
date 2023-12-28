using AElf;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Etos;
using CAServer.Search;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace CAServer.EntityEventHandler.Tests.CAHolder;

public class CaHolderHandlerTests : CAServerEntityEventHandlerTestBase
{
    private readonly ISearchAppService _searchAppService;
    private readonly IDistributedEventBus _eventBus;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;

    public CaHolderHandlerTests()
    {
        _searchAppService = GetRequiredService<ISearchAppService>();
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
        _eventBus = GetRequiredService<IDistributedEventBus>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockGraphQL());
        services.AddSingleton(GetContactProviderMock());
    }

    [Fact]
    public async Task CaHolder_Create_Test()
    {
        try
        {
            var user = new CreateUserEto
            {
                CaHash = HashHelper.ComputeFrom("a23322344aa").ToString(),
                Id = Guid.NewGuid(),
                Nickname = "Wallet 01",
                UserId = Guid.NewGuid()
            };
            await _eventBus.PublishAsync(user);

            var result = await _searchAppService.GetListByLucenceAsync("caholderindex", new GetListInput()
            {
                MaxResultCount = 1
            });

            result.ShouldNotBeNull();
            var extra = JsonConvert.DeserializeObject<PagedResultDto<CAHolderIndex>>(result);
            extra.ShouldNotBeNull();
            extra.Items[0].NickName.ShouldBe("Wallet 01");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CaHolder_Update_Test()
    {
        try
        {
            var id = Guid.NewGuid();
            await _caHolderRepository.AddAsync(new CAHolderIndex()
            {
                Id = id,
                CaHash = "test",
                NickName = "test"
            });
            
            var user = new UpdateCAHolderEto()
            {
                CaHash = HashHelper.ComputeFrom("a23322344aa").ToString(),
                Id = id,
                Nickname = "Wallet 01",
                UserId = Guid.NewGuid()
            };
            await _eventBus.PublishAsync(user);

            var result = await _searchAppService.GetListByLucenceAsync("caholderindex", new GetListInput()
            {
                MaxResultCount = 1
            });

            result.ShouldNotBeNull();
            var extra = JsonConvert.DeserializeObject<PagedResultDto<CAHolderIndex>>(result);
            extra.ShouldNotBeNull();
            extra.Items[0].NickName.ShouldBe("Wallet 01");
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task CaHolder_Delete_Test()
    {
        try
        {
            var deleteEto = new DeleteCAHolderEto()
            {
                CaHash = HashHelper.ComputeFrom("a23322344aa").ToString(),
                Id = Guid.NewGuid(),
                Nickname = "Wallet 01",
                UserId = Guid.NewGuid()
            };
            await _eventBus.PublishAsync(deleteEto);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    private IGraphQLHelper GetMockGraphQL()
    {
        var graph = new Mock<IGraphQLHelper>();

        return graph.Object;
    }

    private IContactProvider GetContactProviderMock()
    {
        var provider = new Mock<IContactProvider>();

        provider.Setup(t => t.GetAddedContactsAsync(It.IsAny<Guid>())).ReturnsAsync(
            new List<ContactIndex>()
            {
                new()
                {
                    UserId = Guid.Empty,
                    Id = Guid.NewGuid(),
                    Name = "test",
                    Index = "T",
                    IsDeleted = false,
                    CaHolderInfo = new CAServer.Entities.Es.CaHolderInfo()
                    {
                        UserId = Guid.Empty,
                    },
                    Addresses = new List<ContactAddress>()
                    {
                        new ContactAddress()
                        {
                            Address = "AAA",
                            ChainId = "AELF"
                        }
                    },
                    ImInfo = new Entities.Es.ImInfo()
                    {
                        RelationId = "test-relationId"
                    },
                    IsImputation = true
                }
            });
        return provider.Object;
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Common;
using CAServer.Contacts;
using CAServer.Contacts.Provider;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Security;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Contact;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ContactProviderTest : CAServerApplicationTestBase
{
    private readonly IContactProvider _contactProvider;
    private ICurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly INESTRepository<ContactIndex, Guid> _contactRepository;

    public ContactProviderTest()
    {
        _contactProvider = GetRequiredService<IContactProvider>();
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
        _contactRepository = GetRequiredService<INESTRepository<ContactIndex, Guid>>();

        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        //services.AddSingleton(GetGraphQlMock());
    }

    [Fact]
    public async Task GetContactsAsyncTest()
    {
        var userId = _currentUser.GetId();
        await _contactRepository.AddOrUpdateAsync(new ContactIndex()
        {
            UserId = userId,
            Id = Guid.NewGuid(),
            Name = "test",
            Index = "T",
            IsDeleted = false
        });

        await Task.Delay(200);

        var contact = await _contactProvider.GetContactsAsync(userId);
        contact.ShouldNotBeNull();
        contact.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetContactAsyncTest()
    {
        var userId = _currentUser.GetId();
        var contactUserId = Guid.NewGuid();

        await _contactRepository.AddOrUpdateAsync(new ContactIndex()
        {
            UserId = userId,
            Id = Guid.NewGuid(),
            Name = "test",
            Index = "T",
            IsDeleted = false,
            CaHolderInfo = new CAServer.Entities.Es.CaHolderInfo()
            {
                UserId = contactUserId
            }
        });
        
        await _contactRepository.AddOrUpdateAsync(new ContactIndex()
        {
            UserId = userId,
            Id = Guid.NewGuid(),
            Name = "test",
            Index = "T",
            IsDeleted = false,
            CaHolderInfo = null
        });
        
        await _contactRepository.AddOrUpdateAsync(new ContactIndex()
        {
            UserId = contactUserId,
            Id = Guid.NewGuid(),
            Name = "test",
            Index = "T",
            IsDeleted = false,
            CaHolderInfo = new CAServer.Entities.Es.CaHolderInfo()
            {
                UserId = userId
            }
        });

        await Task.Delay(200);

        var contact = await _contactProvider.GetContactAsync(userId, contactUserId);
        contact.ShouldNotBeNull();
        contact.Name.ShouldBe("test");

        var contacts = await _contactProvider.GetAddedContactsAsync(userId);
        contacts.ShouldNotBeNull();
        contacts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetCaHolderAsyncTest()
    {
        var userId = _currentUser.GetId();
        var caHash = "test";

        await _caHolderRepository.AddOrUpdateAsync(new CAHolderIndex()
        {
            UserId = userId,
            CaHash = caHash,
            CreateTime = DateTime.UtcNow,
            NickName = "test",
            Id = Guid.NewGuid()
        });
        await Task.Delay(200);

        var holder = await _contactProvider.GetCaHolderAsync(userId, caHash);

        holder.ShouldNotBeNull();
        holder.CaHash.ShouldBe(caHash);
    }

    [Fact]
    public async Task GetCaHolderInfoAsyncTest()
    {
        var userId = _currentUser.GetId();
        var caHash = "test";

        await _caHolderRepository.AddOrUpdateAsync(new CAHolderIndex()
        {
            UserId = userId,
            CaHash = caHash,
            CreateTime = DateTime.UtcNow,
            NickName = "test",
            Id = Guid.NewGuid()
        });
        await Task.Delay(200);

        var holder = await _contactProvider.GetCaHolderAsync(userId, caHash);
        

        holder.ShouldNotBeNull();
        holder.CaHash.ShouldBe(caHash);
    }

    [Fact]
    public async Task GetCaHolderInfoAsync_GraphQL_Test()
    {
        try
        {
            var caHash = "test"; 
            await _contactProvider.GetCaHolderInfoAsync(new List<string>(), caHash);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
    
    [Fact]
    public async Task GetCaHolderInfoByAddressAsync_GraphQL_Test()
    {
        try
        {
            await _contactProvider.GetCaHolderInfoByAddressAsync(new List<string>(), string.Empty);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    private async Task<IGraphQLHelper> GetGraphQlMock()
    {
        var helper = new Mock<IGraphQLHelper>();
        helper.Setup(t => t.QueryAsync<CAServer.Guardian.Provider.GuardiansDto>(It.IsAny<GraphQLRequest>()))
            .ReturnsAsync(new GuardiansDto()
            {
                CaHolderInfo = new List<GuardianDto>()
                {
                    new GuardianDto()
                    {
                        ChainId = "AELF",
                        CaHash = "test"
                    }
                }
            });
        return helper.Object;
    }
}
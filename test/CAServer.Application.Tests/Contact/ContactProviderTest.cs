using System;
using System.Collections.Generic;
using System.Linq;
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
            IsDeleted = false,
            IsImputation = true
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

        var contactsAddresses = await _contactProvider.GetContactByAddressesAsync(userId, new List<string>() { "AAA" });
        contactsAddresses.ShouldNotBeNull();
        contactsAddresses.Count.ShouldBe(1);
        
        var relation = await _contactProvider.GetContactByRelationIdAsync(userId, "test-relationId");
        relation.ShouldNotBeNull();
        relation.ImInfo.RelationId.ShouldBe("test-relationId");
        
        var contactsAddress = await _contactProvider.GetContactByAddressAsync(userId, "AAA");
        contactsAddress.ShouldNotBeNull();
        contactsAddress.Name.ShouldBe("test");
        
        var contactsImputation = await _contactProvider.GetImputationAsync(userId);
        contactsImputation.ShouldBeTrue();
        
        var contactsImputationFalse = await _contactProvider.GetImputationAsync(Guid.NewGuid());
        contactsImputationFalse.ShouldBeFalse();
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

        var holders = await _contactProvider.GetCaHoldersAsync(new List<Guid>() { userId });
        holders.ShouldNotBeNull();
        holders.Count.ShouldBe(1);
        holders.First().NickName.ShouldBe("test");
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
    
    [Fact]
    public async Task GetContactList_Provider_Test()
    {
        var newGuid = Guid.NewGuid();
        _contactRepository.AddOrUpdateAsync(new ContactIndex()
        {
            UserId = _currentUser.GetId(),
            CaHolderInfo = new Entities.Es.CaHolderInfo()
            {
                UserId = newGuid,
            },
            Addresses = new List<ContactAddress>()
            {
                new ContactAddress()
                {
                    Address = "aaa"
                }
            },
            IsDeleted = false
        });
        
        var list = await _contactProvider.GetContactListAsync(new List<string>()
        {
            newGuid.ToString()
        },"", _currentUser.GetId());

        list.ShouldNotBeNull();

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
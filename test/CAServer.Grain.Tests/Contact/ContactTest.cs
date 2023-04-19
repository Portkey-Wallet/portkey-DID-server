using CAServer.Contacts;
using CAServer.Grains.Grain.Contacts;
using Shouldly;
using Xunit;

namespace CAServer.Grain.Tests.Contact;

[Collection(ClusterCollection.Name)]
public class ContactTest : CAServerGrainTestBase
{
    private const string DefaultName = "Tom";

    [Fact]
    public async Task AddContactTest()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = DefaultName,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        var result = await grain.AddContactAsync(userId, contact);
        result.Success.ShouldBeTrue();
        result.Data.Name.ShouldBe(DefaultName);
    }

    [Fact]
    public async Task UpdateContactTest()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = DefaultName,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        await grain.AddContactAsync(userId, contact);

        contact.Name = "John";
        var result = await grain.UpdateContactAsync(userId, contact);
        result.Success.ShouldBeTrue();
        result.Data.Name.ShouldBe("John");
    }
    
    [Fact]
    public async Task DeleteContactTest()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = DefaultName,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        await grain.AddContactAsync(userId, contact);

        var result = await grain.DeleteContactAsync(userId);
        result.Success.ShouldBeTrue();
        result.Data.IsDeleted.ShouldBeTrue();
    }
}
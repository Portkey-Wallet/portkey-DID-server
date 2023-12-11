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
    public async Task Update_Contact_Name_Test()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = string.Empty,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond,
            CaHolderInfo = new CaHolderInfo()
            {
                WalletName = "test"
            }
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        await grain.AddContactAsync(userId, contact);

        contact.Name = "John";
        var result = await grain.UpdateContactAsync(userId, contact);
        result.Success.ShouldBeTrue();
        result.Data.Name.ShouldBe("John");

        contact.Name = string.Empty;
        var updateSameNameResult = await grain.UpdateContactAsync(userId, contact);
        updateSameNameResult.Success.ShouldBeTrue();
        updateSameNameResult.Data.Name.ShouldBe(string.Empty);
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

    [Fact]
    public async Task UpdateWalletNameTest()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = DefaultName,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond,
            CaHolderInfo = new CaHolderInfo()
            {
                WalletName = ""
            }
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        await grain.AddContactAsync(userId, contact);

        var walletName = "test";

        var result = await grain.UpdateContactInfo(walletName, string.Empty);
        result.Success.ShouldBeTrue();
        result.Data.CaHolderInfo.WalletName.ShouldBe(walletName);
    }

    [Fact]
    public async Task UpdateWalletName_CaHolderInfo_Null_Test()
    {
        var userId = Guid.NewGuid();
        var Id = Guid.NewGuid();
        var contact = new ContactGrainDto
        {
            Id = Guid.NewGuid(),
            Name = DefaultName,
            UserId = userId,
            ModificationTime = DateTime.Now.Millisecond,
            CaHolderInfo = null
        };

        var grain = Cluster.Client.GetGrain<IContactGrain>(Id);
        await grain.AddContactAsync(userId, contact);

        var walletName = "test";

        var result = await grain.UpdateContactInfo(walletName, string.Empty);
        result.Success.ShouldBeFalse();
        result.Message.ShouldBe(ContactMessage.HolderNullMessage);
    }
}
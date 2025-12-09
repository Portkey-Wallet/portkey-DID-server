using System;
using System.Threading.Tasks;
using CAServer.Contacts;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Contacts;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.Contact;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ContactProfileTest : CAServerApplicationTestBase
{
    
    private Guid DefaultId = new("3fe8e56b-e700-123e-8cb4-d014b485c1a9");
    
    
    private readonly IContactAppService _contactAppService;
    private ICurrentUser _currentUser;
    private readonly TestCluster _cluster;

    public ContactProfileTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockContactProvider());
   
    }
    
    [Fact]
    public async Task Get_Contact_Test()
    {
        var guid = Guid.NewGuid();
        var contactGrain = _cluster.Client.GetGrain<IContactGrain>(guid);
        var contactGrainDto = new ContactGrainDto
        {
            Name = "Name"
        };
        await contactGrain.AddContactAsync(_currentUser.GetId(), contactGrainDto);
        var contactResultDto = await _contactAppService.GetAsync(guid);
        contactResultDto.Name.ShouldBe("Name");
    }
    
    [Fact]
    public async Task Get_Id_NullOrEmpty_Test()
    {
        try
        {
            await _contactAppService.GetAsync(Guid.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }
    
    [Fact]
    public async Task Get_Id_Not_Exist_Test()
    {
        try
        {
            await _contactAppService.GetAsync(DefaultId);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }
}
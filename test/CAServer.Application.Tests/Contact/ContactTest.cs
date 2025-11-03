using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using CAServer.Common;
using CAServer.Commons;
using CAServer.Contacts;
using CAServer.Entities.Es;
using CAServer.Grain.Tests;
using CAServer.Grains.Grain.Contacts;
using CAServer.Options;
using CAServer.Security;
using CAServer.ThirdPart.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nethereum.Hex.HexConvertors.Extensions;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.Users;
using Volo.Abp.Validation;
using Xunit;
using Environment = CAServer.Options.Environment;
using ImInfo = CAServer.Contacts.ImInfo;

namespace CAServer.Contact;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public partial class ContactTest : CAServerApplicationTestBase
{
    private const string DefaultName = "Tom";
    public List<ContactAddressDto> Addresses = new List<ContactAddressDto>();
    private const string DefaultChainId = "DefaultChainId";
    private const string DefaultAddress = "DefaultAddress";

    private readonly IContactAppService _contactAppService;
    private ICurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly TestCluster _cluster;

    public ContactTest()
    {
        _contactAppService = GetRequiredService<IContactAppService>();
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
        _cluster = GetRequiredService<ClusterFixture>().Cluster;
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockHttpClient());
        services.AddSingleton(GetMockContactProvider());
        services.AddSingleton(GetMockHostInfoOptions());
        services.AddSingleton(GetMockVariablesOptions());
        services.AddSingleton(GetMockAccessor());
    }

    [Fact]
    public async Task Contact_PipeLine_Success_Test()
    {
        Addresses.Add(new ContactAddressDto
        {
            ChainId = DefaultChainId,
            Address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
        });

        var dto = new CreateUpdateContactDto
        {
            Name = DefaultName,
            Addresses = Addresses
        };

        //create
        var createResult = await _contactAppService.CreateAsync(dto);

        createResult.ShouldNotBeNull();
        createResult.Name.ShouldBe(DefaultName);

        // //update
        var newName = "newName";
        var updateDto = new CreateUpdateContactDto
        {
            Name = newName,
            Addresses = Addresses
        };
        var updateResult = await _contactAppService.UpdateAsync(createResult.Id, updateDto);

        updateResult.ShouldNotBeNull();
        updateResult.Name.ShouldBe(newName);

        //getExist
        var exitResult = await _contactAppService.GetExistAsync(newName);
        exitResult.ShouldNotBeNull();
        exitResult.Existed.ShouldBeTrue();
        updateResult.Name.ShouldBe(newName);

        //delete
        await _contactAppService.DeleteAsync(createResult.Id);
    }

    [Fact]
    public async Task Update_Not_Exist_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = DefaultAddress
            });

            var dto = new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            };

            await _contactAppService.UpdateAsync(Guid.Empty, dto);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }

    [Fact]
    public async Task Get_Not_Exist_Test()
    {
        try
        {
            await _contactAppService.GetExistAsync(string.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }

    [Fact]
    public async Task Delete_Not_Exist_Test()
    {
        try
        {
            await _contactAppService.DeleteAsync(Guid.Empty);
        }
        catch (Exception e)
        {
            e.Message.ShouldBe(ContactMessage.NotExistMessage);
        }
    }


    [Fact]
    public async Task CreateOrUpdate_Body_Empty_Test()
    {
        try
        {
            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Name_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = DefaultAddress
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = "",
                Addresses = new List<ContactAddressDto>()
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Addresses_Empty_Test()
    {
        try
        {
            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = new List<ContactAddressDto>()
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_ChainId_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = "",
                Address = DefaultAddress
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task CreateOrUpdate_Address_NullOrEmpty_Test()
    {
        try
        {
            Addresses.Add(new ContactAddressDto
            {
                ChainId = DefaultChainId,
                Address = ""
            });

            await _contactAppService.CreateAsync(new CreateUpdateContactDto
            {
                Name = DefaultName,
                Addresses = Addresses
            });
        }
        catch (Exception e)
        {
            Assert.True(e is AbpValidationException);
        }
    }

    [Fact]
    public async Task Get_Test()
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
    public async Task GetImputationAsyncTest()
    {
        var result = await _contactAppService.GetImputationAsync();
        result.ShouldNotBeNull();
        result.IsImputation.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadImputation_Fail_Test()
    {
        try
        {
            await _contactAppService.ReadImputationAsync(new ReadImputationDto()
            {
                ContactId = Guid.Empty
            });
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task ReadImputation_Test()
    {
        var userId = _currentUser.GetId();
        var grainId = Guid.NewGuid();
        var contactGrain = _cluster.Client.GetGrain<IContactGrain>(grainId);

        var contact = await contactGrain.AddContactAsync(userId, new ContactGrainDto()
        {
            Id = grainId,
            UserId = userId,
            IsImputation = false
        });
        contact.Data.IsImputation.ShouldBeFalse();

        var contactImputation = await contactGrain.Imputation();
        contactImputation.Data.IsImputation.ShouldBeTrue();

        await _contactAppService.ReadImputationAsync(new ReadImputationDto()
        {
            ContactId = grainId
        });

        contact.Data.IsImputation.ShouldBeFalse();
    }

    [Fact]
    public async Task GetContactTest()
    {
        var userId = _currentUser.GetId();
        var caHolderGrain = _cluster.Client.GetGrain<ICAHolderGrain>(userId);
        await caHolderGrain.AddHolderAsync(new CAHolderGrainDto()
        {
            UserId = userId,
            CaHash = "test",
            CreateTime = DateTime.UtcNow,
            Id = userId,
            Nickname = "test"
        });

        var contact = await _contactAppService.GetContactAsync(userId);

        contact.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetNameTest()
    {
        var names = await _contactAppService.GetNameAsync(new List<Guid>()
            { Guid.Empty, Guid.NewGuid(), Guid.Empty, Guid.NewGuid() });

        names.Count.ShouldNotBe(0);
    }

    [Fact]
    public void GetImTest()
    {
        var dto = new CaHolderInfoDto
        {
            CaHash = "test",
            UserId = _currentUser.GetId(),
            WalletName = "test"
        };

        var holderDto = new CaHolderDto
        {
            CaHash = "test",
            UserId = _currentUser.GetId().ToString(),
            WalletName = "test"
        };

        var imInfos = new ImInfos
        {
            RelationId = string.Empty,
            Name = "test",
            PortkeyId = "test"
        };

        var imDto = new ImInfoDto
        {
            RelationId = string.Empty,
            Name = "test",
            PortkeyId = _currentUser.GetId(),
            AddressWithChain = new List<AddressWithChain>()
            {
                new()
                {
                    Address = "test",
                    ChainName = "test"
                }
            }
        };
    }

    [Fact]
    public async Task IM_Follow_Remark_Success_Test()
    {
        Addresses.Add(new ContactAddressDto
        {
            ChainId = DefaultChainId,
            Address = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58(),
        });

        var dto = new CreateUpdateContactDto
        {
            Name = DefaultName,
            Addresses = Addresses
        };

        //create
        var createResult = await _contactAppService.CreateAsync(dto);

        createResult.ShouldNotBeNull();
        createResult.Name.ShouldBe(DefaultName);

    }

    [Fact]
    public async Task GetContactList_Test()
    {
        var list = await _contactAppService.GetContactListAsync(new ContactListRequestDto());

        list.ShouldNotBeNull();

    }
    private IOptionsSnapshot<HostInfoOptions> GetMockHostInfoOptions()
    {
        var mockOptionsSnapshot = new Mock<IOptionsSnapshot<HostInfoOptions>>();
        mockOptionsSnapshot.Setup(o => o.Value).Returns(
            new HostInfoOptions
            {
                Environment = Environment.Production
            });
        return mockOptionsSnapshot.Object;
    }

    private IHttpContextAccessor GetMockAccessor()
    {
        var mockAccessor = new Mock<IHttpContextAccessor>();
        mockAccessor.Setup(o => o.HttpContext).Returns(
            new DefaultHttpContext());
        return mockAccessor.Object;
    }
    
    private IHttpClientService GetMockHttpClient()
    {
        var mockHttpClient = new Mock<IHttpClientService>();
        mockHttpClient
            .Setup(o => o.GetAsync<CommonResponseDto<ImInfo>>(It.IsAny<string>(),
                It.IsAny<Dictionary<string, string>>())).Returns(Task.FromResult(new CommonResponseDto<ImInfo>
            {
                Code = "20000",
                Data = new ImInfo()
                {
                    Name = "aa"
                }
            }));
        return mockHttpClient.Object;
    }

    private IOptions<VariablesOptions> GetMockVariablesOptions()
    {
        var mockOptions = new Mock<IOptions<VariablesOptions>>();
        mockOptions.Setup(o => o.Value).Returns(
            new VariablesOptions
            {
                ImageMap = new Dictionary<string, string>()
                {
                    ["aelf"] = "aelfImage",
                    ["eth"] = "ethImage"
                }
            });
        return mockOptions.Object;
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Guardian.Provider;
using CAServer.Options;
using CAServer.PrivacyPermission;
using CAServer.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.ImUser;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ListHolderInfoTest : CAServerApplicationTestBase
{
    private readonly CurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;
    private readonly INESTRepository<GuardianIndex, string> _guardianRepository;
    private readonly IImUserAppService _imUserAppService;
    private readonly INESTRepository<UserExtraInfoIndex, string> _userExtraInfoRepository;
    
    public ListHolderInfoTest()
    {
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
        _guardianRepository = GetRequiredService<INESTRepository<GuardianIndex, string>>();
        _imUserAppService = GetRequiredService<IImUserAppService>();
        _userExtraInfoRepository = GetRequiredService<INESTRepository<UserExtraInfoIndex, string>>();
    }
    
    
    protected override void AfterAddApplication(IServiceCollection services)
    {
        base.AfterAddApplication(services);
        services.AddSingleton(GetMockGuardian());
        services.AddSingleton(GetPrivacyPermission());
    }
    
    [Fact]
    public async Task ListHolderInfoAsync_Email_Test()
    {
        var userId = _currentUser.GetId();
        await _caHolderRepository.AddOrUpdateAsync(new CAHolderIndex()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CaHash = "test",
            NickName = "test"
        });
            
        await _guardianRepository.AddOrUpdateAsync(new GuardianIndex()
        {
            Id = Guid.NewGuid().ToString(),
            Identifier = "aaa@test.com",
            IdentifierHash = "IdentifierHash"
        });
        
        await _userExtraInfoRepository.AddOrUpdateAsync(new UserExtraInfoIndex()
        {
            Id = "aaa@test.com",
            Email = "aaa@test.com",
            GuardianType = "Apple"
        });
        await _userExtraInfoRepository.AddOrUpdateAsync(new UserExtraInfoIndex()
        {
            Id = "aaaa",
            Email = "aaa@test.com",
            GuardianType = "aaa"
        });

        await _imUserAppService.ListHolderInfoAsync("aaa@test.com").ShouldNotBeNull();
    }

    private IGuardianProvider GetMockGuardian()
    {
        var provider = new Mock<IGuardianProvider>();

        provider.Setup(t => t.GetGuardiansAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GuardiansDto()
            {
                CaHolderInfo = new List<GuardianDto>()
                {
                    new GuardianDto()
                    {
                        CaAddress = "test",
                        CaHash = "test",
                        ChainId = "AELF"
                    }
                }
            });
        
        return provider.Object;
    }
    
    private IPrivacyPermissionAppService GetPrivacyPermission()
    {
        var provider = new Mock<IPrivacyPermissionAppService>();

        provider.Setup(t => t.CheckPrivacyPermissionAsync(It.IsAny<List<Guid>>(), It.IsAny<string>(), It.IsAny<PrivacyType>()))
            .ReturnsAsync((new List<Guid>
            {
                Guid.Empty
            }, new List<Guid>
            {
                Guid.Empty
            }));
        
        return provider.Object;
    }
}
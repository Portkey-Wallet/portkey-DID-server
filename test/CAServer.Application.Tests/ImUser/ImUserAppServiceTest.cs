using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Entities.Es;
using CAServer.Security;
using Shouldly;
using Volo.Abp.Users;
using Xunit;

namespace CAServer.ImUser;

[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class ImUserAppServiceTest : CAServerApplicationTestBase
{
    private readonly IImUserAppService _imUserAppService;
    private readonly CurrentUser _currentUser;
    private readonly INESTRepository<CAHolderIndex, Guid> _caHolderRepository;

    public ImUserAppServiceTest()
    {
        _imUserAppService = GetRequiredService<IImUserAppService>();
        _currentUser = new CurrentUser(new FakeCurrentPrincipalAccessor());
        _caHolderRepository = GetRequiredService<INESTRepository<CAHolderIndex, Guid>>();
    }

    [Fact]
    public async Task GetHolderInfoAsyncTest()
    {
        try
        {
            var userId = _currentUser.GetId();

            _caHolderRepository.UpdateAsync(new CAHolderIndex()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CaHash = "test",
                NickName = "test"
            });
            await _imUserAppService.GetHolderInfoAsync(userId);
        }
        catch (Exception e)
        {
            e.ShouldNotBeNull();
        }
    }
}
using System.Threading.Tasks;
using CAServer.Bookmark.Dtos;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;

namespace CAServer.Bookmark;

[RemoteService(false), DisableAuditing]
public class BookmarkAppService : CAServerAppService, IBookmarkAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;

    public BookmarkAppService(IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }
    
    public Task CreateAsync(CreateBookmarkDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task DeleteListAsync(DeleteBookmarkDto input)
    {
        throw new System.NotImplementedException();
    }

    public Task SortAsync(SortBookmarksDto input)
    {
        throw new System.NotImplementedException();
    }
}
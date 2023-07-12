using System.Threading.Tasks;
using CAServer.Bookmark.Dtos;
using CAServer.Bookmark.Etos;
using CAServer.Bookmark.Provider;
using CAServer.Entities.Es;
using CAServer.Grains;
using CAServer.Grains.Grain.Bookmark;
using CAServer.Grains.Grain.Bookmark.Dtos;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Tls;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace CAServer.Bookmark;

[RemoteService(false), DisableAuditing]
public class BookmarkAppService : CAServerAppService, IBookmarkAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _eventBus;
    private readonly IBookmarkProvider _bookmarkProvider;
    private IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "CAServer:Bookmark:";

    public BookmarkAppService(IClusterClient clusterClient, IDistributedEventBus eventBus,
        IBookmarkProvider bookmarkProvider, IAbpDistributedLock distributedLock)
    {
        _clusterClient = clusterClient;
        _eventBus = eventBus;
        _bookmarkProvider = bookmarkProvider;
        _distributedLock = distributedLock;
    }

    public async Task CreateAsync(CreateBookmarkDto input)
    {
        var userId = CurrentUser.GetId();

        var metaGrain = GetBookmarkMetaGrain();
        var index = metaGrain.GetTailBookMarkGrainIndex();
        var grain = GetBookmarkGrain(index);

        var grainDto = ObjectMapper.Map<CreateBookmarkDto, BookmarkGrainDto>(input);
        grainDto.UserId = userId;

        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + userId.ToString());

        if (handle != null)
        {
            var addResult = await grain.AddBookMark(grainDto);
            if (!addResult.Success)
            {
                throw new UserFriendlyException(addResult.Message);
            }

            var bookmarkCreateEto = ObjectMapper.Map<BookmarkGrainResultDto, BookmarkCreateEto>(addResult.Data);
            bookmarkCreateEto.UserId = userId;
            await _eventBus.PublishAsync(bookmarkCreateEto);
        }
        else
        {
            Logger.LogError("Do not get lock, keys already exits. userId: {0}", userId.ToString());
        }
    }

    public async Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        var bookmarks = await _bookmarkProvider.GetBookmarksAsync(CurrentUser.GetId(), input);
        return ObjectMapper.Map<PagedResultDto<BookmarkIndex>, PagedResultDto<BookmarkResultDto>>(bookmarks);
    }

    public async Task DeleteAsync()
    {
        var bookMarkMetaGrain = GetBookmarkMetaGrain();
        var bookMarkMetaItems = bookMarkMetaGrain.RemoveAll();
        foreach (var metaItem in bookMarkMetaItems)
        {
            var bookmarkGrain = GetBookmarkGrain(metaItem.GrainIndex);
            await bookmarkGrain.DeleteAll();
        }
        await _eventBus.PublishAsync(new BookmarkDeleteEto { UserId = CurrentUser.GetId() });
    }

    public async Task DeleteListAsync(DeleteBookmarkDto input)
    {
        // 
        var grain = GetBookmarkGrain(0);
        var result = await grain.DeleteItems(input.Ids);
        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _eventBus.PublishAsync(new BookmarkMultiDeleteEto { UserId = CurrentUser.GetId(), Ids = input.Ids });
    }

    public Task SortAsync(SortBookmarksDto input)
    {
        throw new System.NotImplementedException();
    }

    private IBookmarkGrain GetBookmarkGrain(int index)
    {
        var userId = CurrentUser.GetId();
        return _clusterClient.GetGrain<IBookmarkGrain>(
            GrainIdHelper.GenerateGrainId("Bookmark", userId.ToString("N"), index));
    }

    private IBookmarkMetaGrain GetBookmarkMetaGrain()
    {
        var userId = CurrentUser.GetId();
        return _clusterClient.GetGrain<IBookmarkMetaGrain>(
            GrainIdHelper.GenerateGrainId("BookmarkMeta", userId.ToString("N")));
    }
}
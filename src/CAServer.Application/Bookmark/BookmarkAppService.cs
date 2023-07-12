using System.Collections.Generic;
using System.Linq;
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
    private readonly IAbpDistributedLock _distributedLock;
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
        var index = await metaGrain.GetTailBookMarkGrainIndexAsync();
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
            Logger.LogError("Create bookmark fail, do not get lock, keys already exits. userId: {0}",
                userId.ToString());
        }
    }

    public async Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        var bookmarks = await _bookmarkProvider.GetBookmarksAsync(CurrentUser.GetId(), input);
        return ObjectMapper.Map<PagedResultDto<BookmarkIndex>, PagedResultDto<BookmarkResultDto>>(bookmarks);
    }

    public async Task DeleteAsync()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + CurrentUser.GetId().ToString());

        if (handle != null)
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
        else
        {
            Logger.LogError("Delete all bookmarks fail, do not get lock, keys already exits. userId: {0}",
                CurrentUser.GetId().ToString());
        }
    }

    public async Task DeleteListAsync(DeleteBookmarkDto input)
    {
        var grainIndexList = input.Ids
            .GroupBy(i => i.Value)
            .ToDictionary(g => g.Key, g => g.Select(i => i.Key).ToList());
        var grainMetaCountDict = new Dictionary<int, int>();
        foreach (var grainIndexItems in grainIndexList)
        {
            var grain = GetBookmarkGrain(grainIndexItems.Key);
            var result = await grain.DeleteItems(grainIndexItems.Value);
            if (result.Success)
            {
                grainMetaCountDict[grainIndexItems.Key] = result.Data.Count;
            }
        }
        var metaGrain = GetBookmarkMetaGrain();
        await metaGrain.UpdateGrainIndexCountAsync(grainMetaCountDict);
        await _eventBus.PublishAsync(new BookmarkMultiDeleteEto { UserId = CurrentUser.GetId(), Ids = input.Ids.Keys.ToList() });
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
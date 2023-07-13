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
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + userId);
        if (handle == null)
        {
            Logger.LogError("Create bookmark fail, do not get lock, keys already exits. userId: {0}",
                userId.ToString());
            throw new UserFriendlyException("Get lock fail");
        }
        
        var metaGrain = GetBookmarkMetaGrain();
        var index = await metaGrain.GetTailBookMarkGrainIndex();
        var grain = GetBookmarkGrain(index);
        var grainDto = ObjectMapper.Map<CreateBookmarkDto, BookmarkGrainDto>(input);
        grainDto.UserId = userId;
        grainDto.Index = index;

        var addResult = await grain.AddBookMark(grainDto);
        if (!addResult.Success)
        {
            throw new UserFriendlyException(addResult.Message);
        }

        var itemCount = await grain.GetItemCount();
        await metaGrain.UpdateGrainIndexCount(new Dictionary<int, int> { [index] = itemCount });

        var bookmarkCreateEto = ObjectMapper.Map<BookmarkGrainResultDto, BookmarkCreateEto>(addResult.Data);
        bookmarkCreateEto.UserId = userId;
        await _eventBus.PublishAsync(bookmarkCreateEto);
    }

    public async Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        var bookmarks = await _bookmarkProvider.GetBookmarksAsync(CurrentUser.GetId(), input);
        return ObjectMapper.Map<PagedResultDto<BookmarkIndex>, PagedResultDto<BookmarkResultDto>>(bookmarks);
    }

    public async Task DeleteAsync()
    {
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + CurrentUser.GetId());

        if (handle == null)
        {
            Logger.LogError("Delete all bookmarks fail, do not get lock, keys already exits. userId: {0}",
                CurrentUser.GetId().ToString());
            throw new UserFriendlyException("Get lock fail");
        }
        
        var bookMarkMetaGrain = GetBookmarkMetaGrain();
        var bookMarkMetaItems = await bookMarkMetaGrain.RemoveAll();
        foreach (var metaItem in bookMarkMetaItems)
        {
            var bookmarkGrain = GetBookmarkGrain(metaItem.Index);
            await bookmarkGrain.DeleteAll();
        }

        await _eventBus.PublishAsync(new BookmarkDeleteEto { UserId = CurrentUser.GetId() });
    }

    public async Task DeleteListAsync(DeleteBookmarkDto input)
    {
        var grainIndexList = input.Ids
            .GroupBy(i => i.Index)
            .ToDictionary(g => g.Key, g => g.Select(i => i.Id).ToList());
        var grainMetaCountDict = new Dictionary<int, int>();
        
        await using var handle =
            await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix + CurrentUser.GetId());
        if (handle == null)
        {
            Logger.LogError("Delete bookmarks fail, do not get lock, keys already exits. userId: {0}",
                CurrentUser.GetId().ToString());
            throw new UserFriendlyException("Get lock fail");
        }
        
        foreach (var grainIndexItems in grainIndexList)
        {
            var grain = GetBookmarkGrain(grainIndexItems.Key);
            var result = await grain.DeleteItems(grainIndexItems.Value);
            if (result.Success)
            {
                grainMetaCountDict[grainIndexItems.Key] = await grain.GetItemCount();
            }
        }

        var metaGrain = GetBookmarkMetaGrain();
        await metaGrain.UpdateGrainIndexCount(grainMetaCountDict);
        await _eventBus.PublishAsync(new BookmarkMultiDeleteEto
            { UserId = CurrentUser.GetId(), Ids = input.Ids.Select(i => i.Id).ToList() });
    }

    public IBookmarkGrain GetBookmarkGrain(int index)
    {
        var userId = CurrentUser.GetId();
        return _clusterClient.GetGrain<IBookmarkGrain>(
            GrainIdHelper.GenerateGrainId("Bookmark", userId.ToString("N"), index));
    }

    public IBookmarkMetaGrain GetBookmarkMetaGrain()
    {
        var userId = CurrentUser.GetId();
        return _clusterClient.GetGrain<IBookmarkMetaGrain>(
            GrainIdHelper.GenerateGrainId("BookmarkMeta", userId.ToString("N")));
    }
}
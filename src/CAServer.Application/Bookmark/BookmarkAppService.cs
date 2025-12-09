using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CAServer.Bookmark.Dtos;
using CAServer.Grains;
using CAServer.Grains.Grain.Bookmark;
using CAServer.Grains.Grain.Bookmark.Dtos;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Users;

namespace CAServer.Bookmark;

[RemoteService(false), DisableAuditing]
public class BookmarkAppService : CAServerAppService, IBookmarkAppService
{
    private readonly IClusterClient _clusterClient;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "CAServer:Bookmark:";

    public BookmarkAppService(IClusterClient clusterClient,
        IAbpDistributedLock distributedLock)
    {
        _clusterClient = clusterClient;
        _distributedLock = distributedLock;
    }

    public async Task<BookmarkResultDto> CreateAsync(CreateBookmarkDto input)
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

        return ObjectMapper.Map<BookmarkGrainResultDto, BookmarkResultDto>(addResult.Data);
    }

    public async Task<PagedResultDto<BookmarkResultDto>> GetBookmarksAsync(GetBookmarksDto input)
    {
        var metaGrain = GetBookmarkMetaGrain();
        var indexCountList = await metaGrain.GetIndexCount();
        var skipCount = input.SkipCount;
        var tailIdx = indexCountList.Count - 1;
        var totalCount = indexCountList.Select(i => i.Item2).Sum();

        // find tailIdx by skipCount as startIndex
        while (tailIdx >= 0 && skipCount >= indexCountList[tailIdx].Item2)
            skipCount -= indexCountList[tailIdx--].Item2;

        // skipped all data
        if (tailIdx < 0)
            return new PagedResultDto<BookmarkResultDto>(totalCount, new List<BookmarkResultDto>());

        // from tail to head
        var resultList = new List<BookmarkResultDto>();
        var pageCount = input.MaxResultCount;
        for (var i = tailIdx; pageCount > 0 && i >= 0; i--)
        {
            var bookmarkGrain = GetBookmarkGrain(indexCountList[i].Item1);
            var itemCount = await bookmarkGrain.GetItemCount();
            if (itemCount < 1) continue;
            var end = i == tailIdx ? itemCount - skipCount - 1 : itemCount - 1;
            var start = Math.Max(0, end - pageCount + 1);
            pageCount -= end - start + 1;
            var subRange = await bookmarkGrain.GetRange(start, end);
            subRange.Reverse();
            resultList.AddRange(subRange);
        }

        return new PagedResultDto<BookmarkResultDto>
        {
            TotalCount = totalCount,
            Items = resultList
        };
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
    }

    public async Task DeleteListAsync(DeleteBookmarkDto input)
    {
        var grainIndexList = input.DeleteInfos
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
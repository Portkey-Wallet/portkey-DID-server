using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Bookmark.Dtos;
using CAServer.Entities.Es;
using Nest;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace CAServer.Bookmark.Provider;

public interface IBookmarkProvider
{
    Task<PagedResultDto<BookmarkIndex>> GetBookmarksAsync(Guid userId, GetBookmarksDto input);
}

public class BookmarkProvider : IBookmarkProvider, ISingletonDependency
{
    private readonly INESTRepository<BookmarkIndex, Guid> _bookmarkRepository;

    public BookmarkProvider(INESTRepository<BookmarkIndex, Guid> bookmarkRepository)
    {
        _bookmarkRepository = bookmarkRepository;
    }

    public async Task<PagedResultDto<BookmarkIndex>> GetBookmarksAsync(Guid userId, GetBookmarksDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<BookmarkIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(userId)));

        QueryContainer Filter(QueryContainerDescriptor<BookmarkIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<BookmarkIndex> s) => s.Descending(t => t.ModificationTime);

        var (totalCount, bookmarkIndices) = await _bookmarkRepository.GetSortListAsync(Filter, sortFunc: Sort,
            limit: input.MaxResultCount, skip: input.SkipCount);
        
        return new PagedResultDto<BookmarkIndex>(totalCount, bookmarkIndices);
    }
}
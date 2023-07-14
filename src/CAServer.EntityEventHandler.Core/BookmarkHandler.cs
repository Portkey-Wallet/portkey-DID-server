using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using CAServer.Bookmark.Etos;
using CAServer.Entities.Es;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace CAServer.EntityEventHandler.Core;

public class BookmarkHandler : IDistributedEventHandler<BookmarkCreateEto>, IDistributedEventHandler<BookmarkDeleteEto>,
    IDistributedEventHandler<BookmarkMultiDeleteEto>, ITransientDependency
{
    private readonly INESTRepository<BookmarkIndex, Guid> _bookmarkRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CAContactHandler> _logger;

    public BookmarkHandler(INESTRepository<BookmarkIndex, Guid> bookmarkRepository,
        IObjectMapper objectMapper,
        ILogger<CAContactHandler> logger)
    {
        _bookmarkRepository = bookmarkRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(BookmarkCreateEto eventData)
    {
        try
        {
            var bookmark = _objectMapper.Map<BookmarkCreateEto, BookmarkIndex>(eventData);
            await _bookmarkRepository.AddOrUpdateAsync(bookmark);
            _logger.LogInformation("Bookmark add success: {0}-{1}", eventData.UserId.ToString(), eventData.Name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Bookmark add error: {message}", JsonConvert.SerializeObject(eventData));
        }
    }


    public async Task HandleEventAsync(BookmarkDeleteEto eventData)
    {
        try
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<BookmarkIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(eventData.UserId)));

            QueryContainer Filter(QueryContainerDescriptor<BookmarkIndex> f) => f.Bool(b => b.Must(mustQuery));

            var (totalCount, bookmarkIndices) = await _bookmarkRepository.GetListAsync(Filter);
            if (totalCount <= 0) return;
            
            await _bookmarkRepository.BulkDeleteAsync(bookmarkIndices);
            _logger.LogInformation("Bookmark delete all success: {0}", eventData.UserId.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Bookmark delete all error: {message}", JsonConvert.SerializeObject(eventData));
        }
    }

    public async Task HandleEventAsync(BookmarkMultiDeleteEto eventData)
    {
        try
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<BookmarkIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.UserId).Value(eventData.UserId)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(eventData.Ids)));
            QueryContainer Filter(QueryContainerDescriptor<BookmarkIndex> f) => f.Bool(b => b.Must(mustQuery));

            var (totalCount, bookmarkIndices) = await _bookmarkRepository.GetListAsync(Filter);
            if (totalCount <= 0) return;

            await _bookmarkRepository.BulkDeleteAsync(bookmarkIndices);
            _logger.LogInformation("Bookmark delete all success: {0}", eventData.UserId.ToString());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Bookmark delete all error: {message}", JsonConvert.SerializeObject(eventData));
        }
    }
}
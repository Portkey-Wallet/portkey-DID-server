using System;
using System.Collections.Generic;
using CAServer.Bookmark.Dtos;
using Volo.Abp.EventBus;

namespace CAServer.Bookmark.Etos;

[EventName("BookmarkSortEto")]
public class BookmarkSortEto
{
    public Guid UserId { get; set; }
    public List<BookmarkSortInfo> SortItems { get; set; }
}
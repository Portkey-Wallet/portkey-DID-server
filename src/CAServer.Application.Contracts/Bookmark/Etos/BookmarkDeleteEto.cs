using System;
using Volo.Abp.EventBus;

namespace CAServer.Bookmark.Etos;

[EventName("BookmarkDeleteEto")]
public class BookmarkDeleteEto
{
    public Guid UserId { get; set; }
}
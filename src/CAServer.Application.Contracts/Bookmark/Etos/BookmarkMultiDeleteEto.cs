using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace CAServer.Bookmark.Etos;

[EventName("BookmarkMultiDeleteEto")]
public class BookmarkMultiDeleteEto
{
    public Guid UserId { get; set; }
    public List<Guid> Ids { get; set; }
}